#include "CSManager.h"
#include "CSManagedGCHandle.h"
#include "CSManagedAssembly.h"
#include "UnrealSharpCore.h"
#include "UObject/Object.h"
#include "CSNamespace.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "CSUnrealSharpSettings.h"
#include "Logging/StructuredLog.h"
#include "Engine/UserDefinedStruct.h"
#include "Utilities/CSClassUtilities.h"

#ifdef __clang__
#pragma clang diagnostic ignored "-Wdangling-assignment"
#endif

UCSManagedAssembly* FindOwningAssemblyGeneric(UField* Object, TMap<FCSObjectID, TObjectPtr<UCSManagedAssembly>>& NativeClassToAssemblyMap, const TMap<FName, TObjectPtr<UCSManagedAssembly>>& Assemblies)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::FindOwningAssemblyGeneric);
	
	if (FCSClassUtilities::IsBlueprintObject(Object))
	{
		return nullptr;
	}

	if (const ICSManagedTypeInterface* ManagedType = Cast<ICSManagedTypeInterface>(Object))
	{
		return ManagedType->GetOwningAssembly();
	}

	const FCSObjectID ObjectID = Object->GetUniqueID();
	if (const TObjectPtr<UCSManagedAssembly>* Assembly = NativeClassToAssemblyMap.FindByHash(ObjectID.Get(), ObjectID))
	{
		return Assembly->Get();
	}

	const FCSFieldName FieldName(Object);
	UCSManagedAssembly* FoundAssembly = nullptr;
	
	for (const TTuple<FName, TObjectPtr<UCSManagedAssembly>>& NameAssemblyKVP : Assemblies)
	{
		UCSManagedAssembly* Assembly = NameAssemblyKVP.Value;
		TSharedPtr<FGCHandle> TypeHandle = Assembly->FindTypeHandle(FieldName);
		
		if (!TypeHandle.IsValid() || TypeHandle->IsNull())
		{
			continue;
		}
		
		NativeClassToAssemblyMap.AddByHash(ObjectID.Get(), ObjectID, Assembly);
		
		FoundAssembly = Assembly;
		break;
	}

	return FoundAssembly;
}

UCSManager* UCSManager::Instance = nullptr;

void UCSManager::Initialize()
{
	GlobalManagedPackage = FindOrAddManagedPackage(FCSNamespace(TEXT("UnrealSharp")));
	
	FCoreDelegates::OnPreExit.AddUObject(this, &UCSManager::OnEnginePreExit);
	GUObjectArray.AddUObjectDeleteListener(this);
	
	InitialAssemblyLoad();
	
	bHasInitialized = true;
	OnInitialized.Broadcast(*this);
}

void UCSManager::NotifyUObjectDeleted(const UObjectBase* Object, int32 Index)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::NotifyUObjectDeleted);
	
	FCSObjectID ObjectID(Index);

	TSharedPtr<FGCHandle> Handle;
	if (!ManagedObjectHandles.RemoveAndCopyValueByHash(ObjectID.Get(), ObjectID, Handle))
	{
		return;
	}

	UCSManagedAssembly* Assembly = FindOwningAssembly(Object->GetClass());
	TSharedPtr<const FGCHandle> AssemblyHandle = Assembly->GetAssemblyHandle();
	
#if WITH_EDITOR
	if (!AssemblyHandle.IsValid())
	{
		return;
	}
#endif
	
	Handle->Dispose(AssemblyHandle->GetHandle());
	 
	TMap<FCSObjectID, TSharedPtr<FGCHandle>> FoundHandles;
	if (!ManagedInterfaceWrapperHandles.RemoveAndCopyValueByHash(Index, Index, FoundHandles))
	{
		return;
	}

	for (const TTuple<FCSObjectID, TSharedPtr<FGCHandle>>& IDToHandleKVP : FoundHandles)
	{
		IDToHandleKVP.Value->Dispose(AssemblyHandle->GetHandle());
	}
}

UPackage* UCSManager::FindOrAddManagedPackage(const FCSNamespace& Namespace)
{
	if (UPackage* NativePackage = Namespace.TryGetAsNativePackage())
	{
		return NativePackage;
	}
	
	FCSNamespace CurrentNamespace = Namespace;
	TArray<FCSNamespace> ParentNamespaces;
	while (true)
	{
		ParentNamespaces.Add(CurrentNamespace);
		if (!CurrentNamespace.GetParentNamespace(CurrentNamespace))
		{
			break;
		}
	}

	UPackage* ParentPackage = nullptr;
	for (int32 i = ParentNamespaces.Num() - 1; i >= 0; i--)
	{
		const FCSNamespace& ParentNamespace = ParentNamespaces[i];
		const FName PackageName = ParentNamespace.GetPackageName();

		for (UPackage* Package : ManagedPackages)
		{
			if (PackageName == Package->GetFName())
			{
				ParentPackage = Package;
				break;
			}
		}

		if (!ParentPackage)
		{
			ParentPackage = NewObject<UPackage>(nullptr, PackageName, RF_Public);
			ParentPackage->SetPackageFlags(PKG_CompiledIn);
			ManagedPackages.Add(ParentPackage);
		}
	}

	return ParentPackage;
}

UPackage* UCSManager::GetPackage(const FCSNamespace& Namespace)
{
	if (GetDefault<UCSUnrealSharpSettings>()->HasNamespaceSupport())
	{
		return FindOrAddManagedPackage(Namespace);
	}
	
	return GetGlobalManagedPackage();
}

bool UCSManager::IsLoadingAnyAssembly() const
{
	bool bIsLoadingAnyAssembly = false;
	for (const TTuple<FName, TObjectPtr<UCSManagedAssembly>>& NameToAssembly : Assemblies)
	{
		UCSManagedAssembly* Assembly = NameToAssembly.Value;
		
		if (!Assembly->IsAssemblyLoading() && Assembly->IsAssemblyLoaded())
		{
			continue;
		}
		
		bIsLoadingAnyAssembly = true;
		break;
	}
	
	return bIsLoadingAnyAssembly;
}

void UCSManager::InitialAssemblyLoad()
{
	TArray<FLoadOrderManifest> Manifests;
	UnrealSharp::Project::DiscoverLoadOrderManifests(Manifests);
	
	UE_LOGFMT(LogUnrealSharp, Display, "Discovered {0} load order manifests.", Manifests.Num());

	for (const FLoadOrderManifest& Manifest : Manifests)
	{
		UE_LOGFMT(LogUnrealSharp, Display, "Loading assemblies from manifest: {0} (Priority: {1}", Manifest.Name, Manifest.Priority);
		
		for (const FString& Path : Manifest.AssemblyPaths)
		{
			LoadAssemblyByPath(Path, Manifest.bCollectible);
		}
	}
}

UCSManagedAssembly* UCSManager::LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible)
{
	UCSManagedAssembly* Assembly;
	
	const FString AssemblyName = FPaths::GetBaseFilename(AssemblyPath);
	if (UCSManagedAssembly* ExistingAssembly = FindAssembly(*AssemblyName))
	{
		if (ExistingAssembly->IsAssemblyLoaded())
		{
			UE_LOGFMT(LogUnrealSharp, Display, "Assembly {0} is already loaded.", AssemblyName);
			return ExistingAssembly;
		}
		
		Assembly = ExistingAssembly;
	}
	else
	{
		Assembly = NewObject<UCSManagedAssembly>(this, *AssemblyName);
		Assembly->Initialize(AssemblyPath, bIsCollectible);
		
		Assemblies.Add(Assembly->GetFName(), Assembly);
	}

	if (!Assembly->LoadAssembly())
	{
		return nullptr;
	}

	UE_LOGFMT(LogUnrealSharp, Display, "Successfully loaded assembly at {0}.", AssemblyPath);
	return Assembly;
}

UCSManagedAssembly* UCSManager::LoadUserAssemblyByName(const FName AssemblyName, bool bIsCollectible)
{
	const FString AssemblyPath = FPaths::Combine(UnrealSharp::Paths::GetUserAssemblyDirectory(), AssemblyName.ToString() + TEXT(".dll"));
	return LoadAssemblyByPath(AssemblyPath, bIsCollectible);
}

UCSManagedAssembly* UCSManager::LoadPluginAssemblyByName(const FName AssemblyName, bool bIsCollectible)
{
	const FString AssemblyPath = FPaths::Combine(UnrealSharp::Paths::GetPluginAssembliesPath(), AssemblyName.ToString() + TEXT(".dll"));
	return LoadAssemblyByPath(AssemblyPath, bIsCollectible);
}

UCSManagedAssembly* UCSManager::FindOwningAssembly(UClass* Class)
{
	UClass* FirstNonBlueprintClass = FCSClassUtilities::GetFirstNonBlueprintClass(Class);
	
	if (ICSManagedTypeInterface* ManagedType = Cast<ICSManagedTypeInterface>(FirstNonBlueprintClass))
	{
		return ManagedType->GetOwningAssembly();
	}
	
	return FindOwningAssemblyGeneric(FirstNonBlueprintClass, NativeTypeToAssembly, Assemblies);
}

UCSManagedAssembly* UCSManager::FindOwningAssembly(UScriptStruct* Struct)
{
	return FindOwningAssemblyGeneric(Struct, NativeTypeToAssembly, Assemblies);
}

UCSManagedAssembly* UCSManager::FindOwningAssembly(UEnum* Enum)
{
	return FindOwningAssemblyGeneric(Enum, NativeTypeToAssembly, Assemblies);
}

FGCHandle UCSManager::FindManagedObject(const UObject* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::FindManagedObject);

	if (!IsValid(Object))
	{
		return FGCHandle::InvalidHandle();
	}

	const FCSObjectID ObjectID = Object->GetUniqueID();
	if (TSharedPtr<FGCHandle>* FoundHandle = ManagedObjectHandles.FindByHash(ObjectID.Get(), ObjectID))
	{
		TSharedPtr<FGCHandle> HandlePtr = *FoundHandle;
		if (HandlePtr.IsValid() && !HandlePtr->IsNull())
		{
			return *HandlePtr;
		}
	}

	UCSManagedAssembly* OwningAssembly = FindOwningAssembly(Object->GetClass());
	if (!IsValid(OwningAssembly))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find assembly for {0}", Object->GetName());
		return FGCHandle::InvalidHandle();
	}

	return *OwningAssembly->CreateManagedObjectFromNative(Object);
}

FGCHandle UCSManager::FindManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass)
{
	if (!Object->GetClass()->ImplementsInterface(InterfaceClass))
	{
		return FGCHandle::InvalidHandle();
	}

	UCSManagedAssembly* OwningAssembly = FindOwningAssembly(InterfaceClass);
	if (!IsValid(OwningAssembly))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find assembly for {0}", InterfaceClass->GetName());
		return FGCHandle::InvalidHandle();
	}
	
	TSharedPtr<FGCHandle> FoundHandle = OwningAssembly->GetOrCreateManagedInterface(Object, InterfaceClass);
	if (!FoundHandle.IsValid())
	{
		return FGCHandle::InvalidHandle();
	}

	return *FoundHandle;
}

void UCSManager::AddOrExecuteOnManagerInitialized(const FCSManagerInitializedEvent::FDelegate& Delegate)
{
	if (bHasInitialized)
	{
		Delegate.Execute(*this);
	}
	else
	{
		OnInitialized.Add(Delegate);
	}
}