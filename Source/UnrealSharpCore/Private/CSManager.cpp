#include "CSManager.h"
#include "CSManagedGCHandle.h"
#include "CSManagedAssembly.h"
#include "UnrealSharpCore.h"
#include "Misc/App.h"
#include "UObject/Object.h"
#include "Misc/MessageDialog.h"
#include "Engine/Blueprint.h"
#include "CSNamespace.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "CSUnrealSharpSettings.h"
#include "Engine/UserDefinedEnum.h"
#include "Logging/StructuredLog.h"

#if ENGINE_MAJOR_VERSION >= 5 && ENGINE_MINOR_VERSION >= 5
#include "StructUtils/UserDefinedStruct.h"
#else
#include "Engine/UserDefinedStruct.h"
#endif

#include "Utilities/CSClassUtilities.h"

#ifdef __clang__
#pragma clang diagnostic ignored "-Wdangling-assignment"
#endif

UCSManager* UCSManager::Instance = nullptr;

void UCSManager::Initialize()
{
	GlobalManagedPackage = FindOrAddManagedPackage(FCSNamespace(TEXT("UnrealSharp")));
	
	FCoreDelegates::OnPreExit.AddUObject(this, &UCSManager::OnEnginePreExit);
	GUObjectArray.AddUObjectDeleteListener(this);
	
	LoadAssemblies();
	
	bHasInitialized = true;
	OnCSManagerInitialized.Broadcast(*this);
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

	TMap<FCSObjectID, TSharedPtr<FGCHandle>>* FoundHandles = ManagedInterfaceWrappers.FindByHash(Index, Index);
	if (!FoundHandles)
	{
		return;
	}

	for (const auto& [Key, Value] : *FoundHandles)
	{
		Value->Dispose(AssemblyHandle->GetHandle());
	}
	
	ManagedInterfaceWrappers.RemoveByHash(Index, Index);
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

		for (UPackage* Package : AllPackages)
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
			AllPackages.Add(ParentPackage);
		}
	}

	return ParentPackage;
}

void UCSManager::ForEachManagedField(const TFunction<void(UObject*)>& Callback) const
{
	for (UPackage* Package : AllPackages)
	{
		ForEachObjectWithPackage(Package, [&Callback](UObject* Object)
		{
			Callback(Object);
			return true;
		}, false);
	}
}

UPackage* UCSManager::GetPackage(const FCSNamespace Namespace)
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
		
		if (!Assembly->IsAssemblyLoading() || Assembly->IsAssemblyLoaded())
		{
			continue;
		}
		
		bIsLoadingAnyAssembly = true;
		break;
	}
	
	return bIsLoadingAnyAssembly;
}

void UCSManager::LoadAssemblies()
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
	const FString AssemblyName = FPaths::GetBaseFilename(AssemblyPath);
	if (UCSManagedAssembly* ExistingAssembly = FindAssembly(*AssemblyName))
	{
		if (ExistingAssembly->IsAssemblyLoaded())
		{
			UE_LOGFMT(LogUnrealSharp, Display, "Assembly {0} is already loaded.", AssemblyName);
			return ExistingAssembly;
		}
	}
	
	UCSManagedAssembly* NewAssembly = NewObject<UCSManagedAssembly>(this, *AssemblyName);
	Assemblies.Add(NewAssembly->GetFName(), NewAssembly);
	
	NewAssembly->Initialize(AssemblyPath, bIsCollectible);

	if (!NewAssembly->LoadAssembly())
	{
		return nullptr;
	}

	UE_LOGFMT(LogUnrealSharp, Display, "Successfully loaded assembly at {0}.", AssemblyPath);
	return NewAssembly;
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
	
	return FindOwningAssemblyGeneric<UClass, UBlueprintGeneratedClass>(FirstNonBlueprintClass);
}

UCSManagedAssembly* UCSManager::FindOwningAssembly(UScriptStruct* Struct)
{
	return FindOwningAssemblyGeneric<UScriptStruct, UUserDefinedStruct>(Struct);
}

UCSManagedAssembly* UCSManager::FindOwningAssembly(UEnum* Enum)
{
	return FindOwningAssemblyGeneric<UEnum, UUserDefinedEnum>(Enum);
}

UCSManagedAssembly* UCSManager::FindOwningAssemblySlow(UField* Field)
{
	const FCSFieldName FieldName(Field);

	for (auto& [Name, Assembly] : Assemblies)
	{
		TSharedPtr<FGCHandle> TypeHandle = Assembly->FindTypeHandle(FieldName);
		
		if (!TypeHandle.IsValid() || TypeHandle->IsNull())
		{
			continue;
		}

		const FCSObjectID ObjectID = Field->GetUniqueID();
		NativeClassToAssemblyMap.AddByHash(ObjectID.Get(), ObjectID, Assembly);
		return Assembly;
	}

	return nullptr;
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

FGCHandle UCSManager::FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass)
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
		OnCSManagerInitialized.Add(Delegate);
	}
}