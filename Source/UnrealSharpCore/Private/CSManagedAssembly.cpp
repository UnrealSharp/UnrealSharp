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

void UCSManagedAssembly::InitializeManagedAssembly(const FStringView InAssemblyPath)
{
	if (!AssemblyFilePath.IsEmpty())
	{
		return;
	}
	
	AssemblyFilePath = FPaths::ConvertRelativePathToFull(InAssemblyPath.GetData());

#if defined(_WIN32)
	// Replace forward slashes with backslashes
	AssemblyFilePath.ReplaceInline(TEXT("/"), TEXT("\\"));
#endif

	AssemblyName = *FPaths::GetBaseFilename(AssemblyFilePath);

	FOnManagedTypeStructureChanged::FDelegate Delegate = FOnManagedTypeStructureChanged::FDelegate::CreateUObject(this, &UCSManagedAssembly::OnTypeReflectionDataChanged);
	FCSManagedTypeDefinitionEvents::AddOnReflectionDataChangedDelegate(Delegate);
}

bool UCSManagedAssembly::LoadManagedAssembly(bool bisCollectible)
{
	TRACE_CPUPROFILER_EVENT_SCOPE_TEXT(*FString(TEXT("UCSManagedAssembly::LoadManagedAssembly: " + AssemblyName.ToString())));

	if (IsValidAssembly())
	{
		UE_LOGFMT(LogUnrealSharp, Display, "{0} is already loaded", *AssemblyName.ToString());
		return true;
	}

	if (!FPaths::FileExists(AssemblyFilePath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Assembly path does not exist: {0}", *AssemblyFilePath);
		return false;
	}

	bIsLoading = true;
	FGCHandle NewAssemblyGCHandle = UCSManager::Get().GetManagedPluginsCallbacks().LoadPlugin(*AssemblyFilePath, bisCollectible);
	NewAssemblyGCHandle.Type = GCHandleType::WeakHandle;

	if (NewAssemblyGCHandle.IsNull())
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to load assembly: {0}", *AssemblyFilePath);
		return false;
	}
	
	AssemblyGCHandle = MakeShared<FGCHandle>(NewAssemblyGCHandle);

	for (TSharedPtr<FCSManagedTypeDefinition>& PendingRebuildType : PendingRebuildTypes)
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
	if (!IsValidAssembly())
	{
		// Assembly is already unloaded.
		UE_LOGFMT(LogUnrealSharp, Display, "{0} is already unloaded", *AssemblyName.ToString());
		return true;
	}
	
	FGCHandleIntPtr AssemblyHandle = AssemblyGCHandle->GetHandle();
	for (TSharedPtr<FGCHandle>& Handle : AllocatedGCHandles)
	{
		Handle->Dispose(AssemblyHandle);
	}

	ManagedTypeGCHandles.Reset();
	AllocatedGCHandles.Reset();

	// Don't need the assembly handle anymore, we use the path to unload the assembly.
	AssemblyGCHandle->Dispose(AssemblyGCHandle->GetHandle());
	AssemblyGCHandle.Reset();

    UCSManager::Get().OnManagedAssemblyUnloadedEvent().Broadcast(this);
	return UCSManager::Get().GetManagedPluginsCallbacks().UnloadPlugin(*AssemblyFilePath);
}

TSharedPtr<FGCHandle> UCSManagedAssembly::FindTypeHandle(const FCSFieldName& FieldName)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::TryFindTypeHandle);
	UE_LOGFMT(LogUnrealSharp, Verbose, "Looking up type handle for {0}", *FieldName.GetFullName().ToString());
	
	if (!IsValidAssembly())
	{
		return nullptr;
	}

	if (TSharedPtr<FGCHandle>* Handle = ManagedTypeGCHandles.Find(FieldName))
	{
		return *Handle;
	}

	FString FullName = FieldName.GetFullName().ToString();
	uint8* TypeHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedType(AssemblyGCHandle->GetPointer(), *FullName);

	if (!TypeHandle)
	{
		return nullptr;
	}
	
	return AddTypeHandle(FieldName, TypeHandle);
}

TSharedPtr<FGCHandle> UCSManagedAssembly::GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::GetManagedMethod);
	UE_LOGFMT(LogUnrealSharp, Verbose, "Looking up managed method {0}", *MethodName);
	
	if (!TypeHandle.IsValid())
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Type handle is invalid for method %s", *MethodName);
		return nullptr;
	}

	uint8* MethodHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedMethod(TypeHandle->GetPointer(), *MethodName);

	if (MethodHandle == nullptr)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Failed to find managed method for %s"), *MethodName);
		return nullptr;
	}

	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(MethodHandle, GCHandleType::WeakHandle);
	AllocatedGCHandles.Add(AllocatedHandle);
	UE_LOGFMT(LogUnrealSharp, Verbose, "Found managed method {0}", *MethodName);
	return AllocatedHandle;
}

TSharedPtr<FCSManagedTypeDefinition> UCSManagedAssembly::RegisterManagedType(char* InFieldName, char* InNamespace, ECSFieldType FieldType, uint8* TypeGCHandle, const char* RawJsonString)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::RegisterManagedType);
	UE_LOGFMT(LogUnrealSharp, Verbose, "Registering type {0}.{1}", InNamespace, InFieldName);
	
	FCSFieldName FieldName(InFieldName, InNamespace);
	TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition = DefinedManagedTypes.FindOrAdd(FieldName);

#if WITH_EDITOR
	if (ManagedTypeDefinition.IsValid() && !FCSUtilities::ShouldReloadDefinition(ManagedTypeDefinition.ToSharedRef(), RawJsonString))
	{
		ManagedTypeDefinition->SetTypeGCHandle(TypeGCHandle);
		return ManagedTypeDefinition;
	}
#endif
	
	TSharedPtr<FCSTypeReferenceReflectionData> ReflectionData;
	UClass* CompilerClass;
	if (!FCSUtilities::ResolveCompilerAndReflectionData(FieldType, CompilerClass, ReflectionData))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to resolve builder and metadata for field type {0} for type {1}", static_cast<uint8>(FieldType), *FieldName.GetFullName().ToString());
	}
	
	ReflectionData->SerializeFromJsonString(RawJsonString);

	if (ManagedTypeDefinition.IsValid())
	{
		ManagedTypeDefinition->SetReflectionData(ReflectionData);
	}
	else
	{
		UCSManagedTypeCompiler* Compiler = CompilerClass->GetDefaultObject<UCSManagedTypeCompiler>();
		ManagedTypeDefinition = FCSManagedTypeDefinition::CreateFromReflectionData(ReflectionData, this, Compiler);
	}
	
	ManagedTypeDefinition->SetTypeGCHandle(TypeGCHandle);
	return ManagedTypeDefinition;
}

TSharedPtr<FGCHandle> UCSManagedAssembly::CreateManagedObjectFromNative(const UObject* Object)
{
	// Only managed/native classes have a C# counterpart. Other types such as Blueprints are not directly represented.
	UClass* Class = FCSClassUtilities::GetFirstNonBlueprintClass(Object->GetClass());
	
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = FindOrAddManagedTypeDefinition(Class);
	TSharedPtr<FGCHandle> TypeGCHandle = ManagedTypeDefinition->GetTypeGCHandle();
	
	return CreateManagedObjectFromNative(Object, TypeGCHandle);
}

TSharedPtr<FGCHandle> UCSManagedAssembly::CreateManagedObjectFromNative(const UObject* Object, const TSharedPtr<FGCHandle>& TypeGCHandle)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::CreateManagedObjectFromNative);
	
	TCHAR* Error = nullptr;
	FGCHandle NewObjectHandle = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeGCHandle->GetPointer(), &Error);
	NewObjectHandle.Type = GCHandleType::StrongHandle;

	if (NewObjectHandle.IsNull())
	{
		// This should never happen. Potential issues: Exceptions in managed code, TypeHandle is invalid.
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to create managed counterpart for {0}:\n{1}", *Object->GetName(), Error);
	}

	TSharedPtr<FGCHandle> Handle = MakeShared<FGCHandle>(NewObjectHandle);
	AllocatedGCHandles.Add(Handle);

	uint32 ObjectID = Object->GetUniqueID();
	UCSManager::Get().ManagedObjectHandles.AddByHash(ObjectID, ObjectID, Handle);
	
	return Handle;
}

TSharedPtr<FGCHandle> UCSManagedAssembly::GetOrCreateManagedInterface(UObject* Object, UClass* InterfaceClass)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedAssembly::GetOrCreateManagedInterface);

	UClass* NonBlueprintClass = FCSClassUtilities::GetFirstNonBlueprintClass(InterfaceClass);
	TSharedPtr<FCSManagedTypeDefinition> ClassInfo = FindOrAddManagedTypeDefinition(NonBlueprintClass);
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetTypeGCHandle();
	
	uint32 ObjectID = Object->GetUniqueID();
    TMap<uint32, TSharedPtr<FGCHandle>>& TypeMap = UCSManager::Get().ManagedInterfaceWrappers.FindOrAddByHash(ObjectID, ObjectID);
	
	uint32 TypeId = InterfaceClass->GetUniqueID();
	if (TSharedPtr<FGCHandle>* Existing = TypeMap.FindByHash(TypeId, TypeId))
	{
		return *Existing;
	}

    TSharedPtr<FGCHandle>* ObjectHandle = UCSManager::Get().ManagedObjectHandles.FindByHash(ObjectID, ObjectID);
	if (ObjectHandle == nullptr)
	{
		return nullptr;
	}
    
	FGCHandle NewManagedObjectWrapper = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObjectWrapper((*ObjectHandle)->GetPointer(), TypeHandle->GetPointer());
	NewManagedObjectWrapper.Type = GCHandleType::StrongHandle;

	if (NewManagedObjectWrapper.IsNull())
	{
		// This should never happen. Potential issues: IL errors, typehandle is invalid.
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to create managed counterpart for {0}", *Object->GetName());
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
		
	PendingRebuildTypes.Add(ManagedTypeDefinition);
}
