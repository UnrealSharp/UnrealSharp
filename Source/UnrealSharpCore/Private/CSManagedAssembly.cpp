#include "CSManagedAssembly.h"
#include "UnrealSharpCore.h"
#include "Misc/Paths.h"
#include "CSManager.h"
#include "Logging/StructuredLog.h"
#include "ReflectionData/CSEnumReflectionData.h"
#include "Types/CSEnum.h"
#include "Compilers/CSManagedDelegateCompiler.h"
#include "Utilities/CSClassUtilities.h"
#include "Utilities/CSUtilities.h"

void UCSManagedAssembly::InitializeManagedAssembly(const FStringView InAssemblyPath, bool bInIsCollectible)
{
	if (!AssemblyFilePath.IsEmpty())
	{
		return;
	}
	
	AssemblyFilePath = FPaths::ConvertRelativePathToFull(InAssemblyPath.GetData());

#if defined(_WIN32)
	AssemblyFilePath.ReplaceInline(TEXT("/"), TEXT("\\"));
#endif
	
	AssemblyName = *FPaths::GetBaseFilename(AssemblyFilePath);
	bIsCollectible = bInIsCollectible;

	FOnManagedTypeStructureChanged::FDelegate Delegate = FOnManagedTypeStructureChanged::FDelegate::CreateUObject(this, &UCSManagedAssembly::OnTypeReflectionDataChanged);
	FCSManagedTypeDefinitionEvents::AddOnReflectionDataChangedDelegate(Delegate);
}

bool UCSManagedAssembly::LoadManagedAssembly()
{
	TRACE_CPUPROFILER_EVENT_SCOPE_TEXT(*FString(TEXT("UCSManagedAssembly::LoadManagedAssembly: ") + AssemblyName.ToString()));

	if (IsAssemblyLoaded())
	{
		UE_LOGFMT(LogUnrealSharp, Display, "{0} is already loaded", AssemblyName);
		return true;
	}

	if (!FPaths::FileExists(AssemblyFilePath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Assembly path does not exist: {0}", AssemblyFilePath);
		return false;
	}

	bIsLoading = true;

	FGCHandle NewAssemblyGCHandle = UCSManager::Get().GetManagedPluginsCallbacks().LoadPlugin(*AssemblyFilePath, bIsCollectible);
	NewAssemblyGCHandle.Type = GCHandleType::WeakHandle;

	if (NewAssemblyGCHandle.IsNull())
	{
		bIsLoading = false;
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to load assembly: {0}", AssemblyFilePath);
		return false;
	}
	
	AssemblyGCHandle = MakeShared<FGCHandle>(NewAssemblyGCHandle);

	for (const TSharedPtr<FCSManagedTypeDefinition>& PendingRebuildType : PendingRebuildTypes)
	{
		PendingRebuildType->CompileAndGetDefinitionField();
	}

	PendingRebuildTypes.Reset();
	bIsLoading = false;
	
	UCSManager::Get().OnManagedAssemblyLoadedEvent().Broadcast(this);
	return true;
}

bool UCSManagedAssembly::UnloadManagedAssembly()
{
	if (!bIsCollectible)
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Assembly {0} is not collectible and will not be unloaded. It will be unloaded when the editor shuts down.", AssemblyName);
		return true;
	}
	
	if (!IsAssemblyLoaded())
	{
		UE_LOGFMT(LogUnrealSharp, Display, "{0} is already unloaded", AssemblyName);
		return true;
	}
	
	const FGCHandleIntPtr AssemblyHandle = AssemblyGCHandle->GetHandle();
	for (const TSharedPtr<FGCHandle>& Handle : AllocatedGCHandles)
	{
		Handle->Dispose(AssemblyHandle);
	}

	ManagedTypeGCHandles.Reset();
	AllocatedGCHandles.Reset();

	AssemblyGCHandle->Dispose(AssemblyGCHandle->GetHandle());
	AssemblyGCHandle.Reset();

	UCSManager::Get().OnManagedAssemblyUnloadedEvent().Broadcast(this);
	return UCSManager::Get().GetManagedPluginsCallbacks().UnloadPlugin(*AssemblyFilePath);
}

TSharedPtr<FGCHandle> UCSManagedAssembly::FindTypeHandle(const FCSFieldName& FieldName)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::FindTypeHandle);
	UE_LOGFMT(LogUnrealSharp, Verbose, "Looking up type handle for {0}", FieldName.GetFullName());

	if (const TSharedPtr<FGCHandle>* Handle = ManagedTypeGCHandles.Find(FieldName))
	{
		return *Handle;
	}

	const FString FullName = FieldName.GetFullName().ToString();
	uint8* TypeHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedType(AssemblyGCHandle->GetPointer(), *FullName);

	if (!TypeHandle)
	{
		return nullptr;
	}
	
	return AddTypeHandle(FieldName, TypeHandle);
}

TSharedPtr<FGCHandle> UCSManagedAssembly::AddTypeHandle(const FCSFieldName& FieldName, uint8* TypeHandle)
{
	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(TypeHandle, GCHandleType::WeakHandle);
	AllocatedGCHandles.Add(AllocatedHandle);
	ManagedTypeGCHandles.Add(FieldName, AllocatedHandle);
	return AllocatedHandle;
}

TSharedPtr<FGCHandle> UCSManagedAssembly::GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::GetManagedMethod);
	UE_LOGFMT(LogUnrealSharp, Verbose, "Looking up managed method {0}", MethodName);
	
	if (!TypeHandle.IsValid())
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Type handle is invalid for method {0}", MethodName);
		return nullptr;
	}

	uint8* MethodHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedMethod(TypeHandle->GetPointer(), *MethodName);

	if (!MethodHandle)
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find managed method for {0}", MethodName);
		return nullptr;
	}

	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(MethodHandle, GCHandleType::WeakHandle);
	AllocatedGCHandles.Add(AllocatedHandle);
	return AllocatedHandle;
}

TSharedPtr<FCSManagedTypeDefinition> UCSManagedAssembly::FindOrAddManagedTypeDefinition(UClass* Field)
{
	if (const ICSManagedTypeInterface* ManagedClass = FCSClassUtilities::GetManagedType(Field))
	{
		return ManagedClass->GetManagedTypeDefinition();
	}	

	const FCSFieldName FieldName(Field);
	return FindOrAddManagedTypeDefinition(FieldName);
}

TSharedPtr<FCSManagedTypeDefinition> UCSManagedAssembly::FindOrAddManagedTypeDefinition(const FCSFieldName& ClassName)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindOrAddManagedTypeDefinition);

	TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition = DefinedManagedTypes.FindOrAdd(ClassName);

	if (ManagedTypeDefinition.IsValid())
	{
		return ManagedTypeDefinition;
	}

	UField* Field = FCSUtilities::FindField<UField>(ClassName);
	if (!IsValid(Field))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find native class: {0}", ClassName.GetName());
		return nullptr;
	}

	ManagedTypeDefinition = FCSManagedTypeDefinition::CreateFromNativeField(Field, this);
	return ManagedTypeDefinition;
}

TSharedPtr<FCSManagedTypeDefinition> UCSManagedAssembly::FindManagedTypeDefinition(const FCSFieldName& FieldName) const
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindManagedTypeDefinition);

	const TSharedPtr<FCSManagedTypeDefinition>* ManagedTypeDefinition = DefinedManagedTypes.Find(FieldName);
	if (!ManagedTypeDefinition || !ManagedTypeDefinition->IsValid())
	{
		return nullptr;
	}

	return *ManagedTypeDefinition;
}

TSharedPtr<FCSManagedTypeDefinition> UCSManagedAssembly::RegisterManagedType(TCHAR* InFieldName, const TCHAR* InNamespace, ECSFieldType FieldType, uint8* TypeGCHandle, TCHAR* NewJsonReflectionData)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::RegisterManagedType);
	UE_LOGFMT(LogUnrealSharp, Verbose, "Registering type {0}.{1}", InNamespace, InFieldName);
	
	const FCSFieldName FieldName(InFieldName, InNamespace);
	TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition = DefinedManagedTypes.FindOrAdd(FieldName);

#if WITH_EDITOR
	if (ManagedTypeDefinition.IsValid() && !FCSUtilities::ShouldReloadDefinition(ManagedTypeDefinition.ToSharedRef(), NewJsonReflectionData))
	{
		ManagedTypeDefinition->SetTypeGCHandle(TypeGCHandle);
		ManagedTypeDefinition->SetDirtyFlags(None);
		return ManagedTypeDefinition;
	}
#endif
	
	UCSManagedTypeCompiler* Compiler = FCSUtilities::ResolveCompilerFromFieldType(FieldType);
	if (!IsValid(Compiler))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to resolve compiler for field type {0} for type {1}", static_cast<uint8>(FieldType), FieldName.GetFullName());
	}
	
	TSharedPtr<FCSTypeReferenceReflectionData> NewReflectionData = Compiler->CreateNewReflectionData();
	NewReflectionData->SerializeFromJsonString(NewJsonReflectionData);

	if (ManagedTypeDefinition.IsValid())
	{
		ManagedTypeDefinition->SetReflectionData(NewReflectionData);
	}
	else
	{
		ManagedTypeDefinition = FCSManagedTypeDefinition::CreateFromReflectionData(NewReflectionData, this, Compiler);
	}
	
	ManagedTypeDefinition->SetTypeGCHandle(TypeGCHandle);
	return ManagedTypeDefinition;
}

TSharedPtr<FGCHandle> UCSManagedAssembly::CreateManagedObjectFromNative(const UObject* Object)
{
	UClass* Class = FCSClassUtilities::GetFirstNonBlueprintClass(Object->GetClass());
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = FindOrAddManagedTypeDefinition(Class);
	return CreateManagedObjectFromNative(Object, ManagedTypeDefinition->GetTypeGCHandle());
}

TSharedPtr<FGCHandle> UCSManagedAssembly::CreateManagedObjectFromNative(const UObject* Object, const TSharedPtr<FGCHandle>& TypeGCHandle)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::CreateManagedObjectFromNative);
	
	TCHAR* Error = nullptr;
	FGCHandle NewObjectHandle = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeGCHandle->GetPointer(), &Error);
	NewObjectHandle.Type = GCHandleType::StrongHandle;

	if (NewObjectHandle.IsNull())
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to create managed counterpart for {0}:\n{1}", Object->GetName(), Error);
	}

	TSharedPtr<FGCHandle> Handle = MakeShared<FGCHandle>(NewObjectHandle);
	AllocatedGCHandles.Add(Handle);

	const uint32 ObjectID = Object->GetUniqueID();
	UCSManager::Get().ManagedObjectHandles.AddByHash(ObjectID, ObjectID, Handle);
	
	return Handle;
}

TSharedPtr<FGCHandle> UCSManagedAssembly::GetOrCreateManagedInterface(UObject* Object, UClass* InterfaceClass)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::GetOrCreateManagedInterface);

	UClass* NonBlueprintClass = FCSClassUtilities::GetFirstNonBlueprintClass(InterfaceClass);
	TSharedPtr<FCSManagedTypeDefinition> ClassInfo = FindOrAddManagedTypeDefinition(NonBlueprintClass);
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetTypeGCHandle();
	
	const uint32 ObjectID = Object->GetUniqueID();
	TMap<uint32, TSharedPtr<FGCHandle>>& TypeMap = UCSManager::Get().ManagedInterfaceWrappers.FindOrAddByHash(ObjectID, ObjectID);
	
	const uint32 TypeId = InterfaceClass->GetUniqueID();
	if (TSharedPtr<FGCHandle>* Existing = TypeMap.FindByHash(TypeId, TypeId))
	{
		return *Existing;
	}

	TSharedPtr<FGCHandle>* ObjectHandle = UCSManager::Get().ManagedObjectHandles.FindByHash(ObjectID, ObjectID);
	if (!ObjectHandle)
	{
		return nullptr;
	}
    
	FGCHandle NewManagedObjectWrapper = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObjectWrapper((*ObjectHandle)->GetPointer(), TypeHandle->GetPointer());
	NewManagedObjectWrapper.Type = GCHandleType::StrongHandle;

	if (NewManagedObjectWrapper.IsNull())
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to create managed counterpart for {0}", Object->GetName());
	}

	TSharedPtr<FGCHandle> Handle = MakeShared<FGCHandle>(NewManagedObjectWrapper);
	AllocatedGCHandles.Add(Handle);
	
	TypeMap.AddByHash(TypeId, TypeId, Handle);
	return Handle;
}

void UCSManagedAssembly::OnTypeReflectionDataChanged(TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition)
{
	if (ManagedTypeDefinition->GetOwningAssembly() != this)
	{
		return;
	}
		
	PendingRebuildTypes.Add(MoveTemp(ManagedTypeDefinition));
}