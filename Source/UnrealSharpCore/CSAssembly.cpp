#include "CSAssembly.h"
#include "UnrealSharpCore.h"
#include "Misc/Paths.h"
#include "CSManager.h"
#include "Logging/StructuredLog.h"
#include "MetaData/CSClassMetaData.h"
#include "MetaData/CSEnumMetaData.h"
#include "TypeGenerator/CSEnum.h"
#include "TypeGenerator/CSScriptStruct.h"
#include "Utils/CSClassUtilities.h"

void UCSAssembly::InitializeAssembly(const FStringView InAssemblyPath)
{
	if (!AssemblyPath.IsEmpty())
	{
		return;
	}
	
	AssemblyPath = FPaths::ConvertRelativePathToFull(InAssemblyPath.GetData());

#if defined(_WIN32)
	// Replace forward slashes with backslashes
	AssemblyPath.ReplaceInline(TEXT("/"), TEXT("\\"));
#endif

	AssemblyName = *FPaths::GetBaseFilename(AssemblyPath);
}

bool UCSAssembly::LoadAssembly(bool bisCollectible)
{
	TRACE_CPUPROFILER_EVENT_SCOPE_TEXT(*FString(TEXT("UCSAssembly::LoadAssembly: " + AssemblyName.ToString())));

	if (IsValidAssembly())
	{
		UE_LOG(LogUnrealSharp, Display, TEXT("%s is already loaded"), *AssemblyPath);
		return true;
	}

	if (!FPaths::FileExists(AssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Display, TEXT("%s doesn't exist"), *AssemblyPath);
		return false;
	}

	bIsLoading = true;
	FGCHandle NewHandle = UCSManager::Get().GetManagedPluginsCallbacks().LoadPlugin(*AssemblyPath, bisCollectible);
	NewHandle.Type = GCHandleType::WeakHandle;

	if (NewHandle.IsNull())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load: %s"), *AssemblyPath);
		return false;
	}
	
	ManagedAssemblyHandle = MakeShared<FGCHandle>(NewHandle);

	for (TSharedPtr<FCSManagedTypeInfo>& TypeInfo : PendingRebuild)
	{
		TypeInfo->GetOrBuildType();
	}

	PendingRebuild.Reset();

	bIsLoading = false;
	UCSManager::Get().OnManagedAssemblyLoadedEvent().Broadcast(AssemblyName);
	return true;
}

bool UCSAssembly::UnloadAssembly()
{
	if (!IsValidAssembly())
	{
		// Assembly is already unloaded.
		UE_LOGFMT(LogUnrealSharp, Display, "{0} is already unloaded", *AssemblyName.ToString());
		return true;
	}

	TRACE_CPUPROFILER_EVENT_SCOPE_TEXT(*FString(TEXT("UCSAssembly::UnloadAssembly: " + AssemblyName.ToString())));

	FGCHandleIntPtr AssemblyHandle = ManagedAssemblyHandle->GetHandle();
	for (TSharedPtr<FGCHandle> Handle : AllocatedManagedHandles)
	{
		Handle->Dispose(AssemblyHandle);
	}

	ManagedClassHandles.Reset();
	AllocatedManagedHandles.Reset();

	// Don't need the assembly handle anymore, we use the path to unload the assembly.
	ManagedAssemblyHandle->Dispose(ManagedAssemblyHandle->GetHandle());
	ManagedAssemblyHandle.Reset();

    UCSManager::Get().OnManagedAssemblyUnloadedEvent().Broadcast(AssemblyName);
	return UCSManager::Get().GetManagedPluginsCallbacks().UnloadPlugin(*AssemblyPath);
}

TSharedPtr<FGCHandle> UCSAssembly::TryFindTypeHandle(const FCSFieldName& FieldName)
{
	if (!IsValidAssembly())
	{
		return nullptr;
	}

	if (TSharedPtr<FGCHandle>* Handle = ManagedClassHandles.Find(FieldName))
	{
		return *Handle;
	}

	FString FullName = FieldName.GetFullName().ToString();
	uint8* TypeHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedType(ManagedAssemblyHandle->GetPointer(), *FullName);

	if (!TypeHandle)
	{
		return nullptr;
	}
	
	return RegisterTypeHandle(FieldName, TypeHandle);
}

TSharedPtr<FGCHandle> UCSAssembly::GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName)
{
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
	AllocatedManagedHandles.Add(AllocatedHandle);
	return AllocatedHandle;
}

TSharedPtr<FCSManagedTypeInfo> UCSAssembly::TryRegisterType(TCHAR* InFieldName,
	TCHAR* InNamespace,
	int64 LastModifiedTime,
	ECSFieldType FieldType,
	uint8* TypeHandle,
	bool& NeedsRebuild)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::TryRegisterType);
	
	FCSFieldName FieldName(InFieldName, InNamespace);
	TSharedPtr<FCSManagedTypeInfo> TypeInfo = FindTypeInfo(FieldName);

#if WITH_EDITOR
	if (TypeInfo.IsValid() && TypeInfo->GetLastModifiedTime() == LastModifiedTime)
	{
		TypeInfo->SetTypeHandle(TypeHandle);
		NeedsRebuild = false;
		return TypeInfo;
	}
#endif
	
	TSharedPtr<FCSTypeReferenceMetaData> NewMetaData;
	UClass* FieldClass;
	switch (FieldType)
	{
	case ECSFieldType::Class:
		NewMetaData = MakeShared<FCSClassMetaData>();
		FieldClass = UCSClass::StaticClass();
		break;
	case ECSFieldType::Struct:
		NewMetaData = MakeShared<FCSStructMetaData>();
		FieldClass = UCSScriptStruct::StaticClass();
		break;
	case ECSFieldType::Enum:
		NewMetaData = MakeShared<FCSEnumMetaData>();
		FieldClass = UCSEnum::StaticClass();
		break;
	case ECSFieldType::Interface:
		NewMetaData = MakeShared<FCSClassBaseMetaData>();
		FieldClass = UCSInterface::StaticClass();
		break;
	case ECSFieldType::Delegate:
		NewMetaData = MakeShared<FCSClassBaseMetaData>();
		FieldClass = UDelegateFunction::StaticClass();
		break;
	default:
		UE_LOGFMT(LogUnrealSharp, Error, "Unsupported field type: {0}", static_cast<uint8>(FieldType));
		return nullptr;
	}

	NewMetaData->FieldClass = FieldClass;
	NewMetaData->FieldName = FieldName;
	NewMetaData->AssemblyName = AssemblyName;

	if (TypeInfo.IsValid())
	{
		TypeInfo->SetTypeMetaData(NewMetaData);
	}
	else
	{
		TypeInfo = MakeShared<FCSManagedTypeInfo>(NewMetaData, this);
		AllTypes.Add(FieldName, TypeInfo);
	}

#if WITH_EDITOR
	TypeInfo->SetLastModifiedTime(LastModifiedTime);
#endif

	TypeInfo->SetTypeHandle(TypeHandle);
	TypeInfo->MarkAsChanged();
	
	NeedsRebuild = true;
	return TypeInfo;
}

TSharedPtr<FGCHandle> UCSAssembly::CreateManagedObject(const UObject* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::CreateManagedObject);
	
	// Only managed/native classes have a C# counterpart.
	UClass* Class = FCSClassUtilities::GetFirstNonBlueprintClass(Object->GetClass());
	TSharedPtr<FCSManagedTypeInfo> TypeInfo = FindOrAddTypeInfo(Class);
	TSharedPtr<FGCHandle> TypeHandle = TypeInfo->GetManagedTypeHandle();

	TCHAR* Error = nullptr;
	FGCHandle NewManagedObject = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeHandle->GetPointer(), &Error);
	NewManagedObject.Type = GCHandleType::StrongHandle;

	if (NewManagedObject.IsNull())
	{
		// This should never happen. Potential issues: IL errors, typehandle is invalid.
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to create managed counterpart for {0}:\n{1}", *Object->GetName(), Error);
		return nullptr;
	}

	TSharedPtr<FGCHandle> Handle = MakeShared<FGCHandle>(NewManagedObject);
	AllocatedManagedHandles.Add(Handle);

	uint32 ObjectID = Object->GetUniqueID();
	UCSManager::Get().ManagedObjectHandles.AddByHash(ObjectID, ObjectID, Handle);

	return Handle;
}

TSharedPtr<FGCHandle> UCSAssembly::FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindOrCreateManagedInterfaceWrapper);

	UClass* NonBlueprintClass = FCSClassUtilities::GetFirstNonBlueprintClass(InterfaceClass);
	TSharedPtr<FCSManagedTypeInfo> ClassInfo = FindOrAddTypeInfo(NonBlueprintClass);
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetManagedTypeHandle();
	
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
		return nullptr;
	}

	TSharedPtr<FGCHandle> Handle = MakeShared<FGCHandle>(NewManagedObjectWrapper);
	AllocatedManagedHandles.Add(Handle);
	
	TypeMap.AddByHash(TypeId, TypeId, Handle);
	return Handle;
}
