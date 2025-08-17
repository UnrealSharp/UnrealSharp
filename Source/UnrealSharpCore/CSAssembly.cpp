#include "CSAssembly.h"
#include "UnrealSharpCore.h"
#include "Misc/Paths.h"
#include "CSManager.h"
#include "CSUnrealSharpSettings.h"
#include "Logging/StructuredLog.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSEnum.h"
#include "TypeGenerator/CSInterface.h"
#include "TypeGenerator/CSScriptStruct.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"
#include "TypeGenerator/Register/MetaData/CSEnumMetaData.h"
#include "TypeGenerator/Register/MetaData/CSInterfaceMetaData.h"
#include "TypeGenerator/Register/MetaData/CSStructMetaData.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"
#include "Utils/CSClassUtilities.h"

void UCSAssembly::SetAssemblyPath(const FStringView InAssemblyPath)
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
	FModuleManager::Get().OnModulesChanged().AddUObject(this, &UCSAssembly::OnModulesChanged);

	if (ProcessTypeMetadata())
	{
		for (const TPair<FCSFieldName, TSharedPtr<FCSManagedTypeInfo>>& NameToTypeInfo : AllTypes)
		{
			NameToTypeInfo.Value->StartBuildingManagedType();
		}
	}

	bIsLoading = false;
	UCSManager::Get().OnManagedAssemblyLoadedEvent().Broadcast(AssemblyName);
	return true;
}

template <typename T, typename MetaDataType>
void RegisterMetaData(UCSAssembly* OwningAssembly, const TSharedPtr<FJsonValue>& MetaData,
	TMap<FCSFieldName,
	TSharedPtr<FCSManagedTypeInfo>>& Map,
	UClass* FieldType,
	TFunction<void(TSharedPtr<FCSManagedTypeInfo>)> OnRebuild = nullptr)
{
	const TSharedPtr<FJsonObject>& MetaDataObject = MetaData->AsObject();

	const FString Name= MetaDataObject->GetStringField(TEXT("Name"));
	const FString Namespace = MetaDataObject->GetStringField(TEXT("Namespace"));
	const FCSFieldName FullName(*Name, *Namespace);

	TSharedPtr<FCSManagedTypeInfo> ExistingValue = Map.FindRef(FullName);

	if (ExistingValue.IsValid())
	{
		// Parse fresh metadata and update the existing info
		MetaDataType NewMeta;
		NewMeta.SerializeFromJson(MetaDataObject);

		if (ExistingValue->GetStructureState() == HasChangedStructure || NewMeta != *ExistingValue->GetTypeMetaData<MetaDataType>())
		{
			TSharedPtr<MetaDataType> MetaDataPtr = MakeShared<MetaDataType>(NewMeta);
			ExistingValue->SetTypeMetaData(MetaDataPtr);
			ExistingValue->SetStructureState(HasChangedStructure);
			
			if (OnRebuild)
			{
				OnRebuild(ExistingValue);
			}
		}
	}
	else
	{
		TSharedPtr<MetaDataType> ParsedMeta = MakeShared<MetaDataType>();
		ParsedMeta->SerializeFromJson(MetaDataObject);
		
		TSharedPtr<T> NewValue = MakeShared<T>(ParsedMeta, OwningAssembly, FieldType);
		Map.Add(FullName, NewValue);
	}
}

bool UCSAssembly::ProcessTypeMetadata()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::ProcessTypeMetadata);

	const FString MetadataPath = FPaths::ChangeExtension(AssemblyPath, "metadata.json");
	if (!FPaths::FileExists(MetadataPath))
	{
		return true;
	}

	FString JsonString;
	if (!FFileHelper::LoadFileToString(JsonString, *MetadataPath))
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load MetaDataPath at: %s"), *MetadataPath);
		return false;
	}

	TSharedPtr<FJsonObject> JsonObject;
	if (!FJsonSerializer::Deserialize(TJsonReaderFactory<>::Create(JsonString), JsonObject) || !JsonObject.IsValid())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to parse JSON at: %s"), *MetadataPath);
		return false;
	}
	
	UCSManager& Manager = UCSManager::Get();

	const TArray<TSharedPtr<FJsonValue>>& StructMetaData = JsonObject->GetArrayField(TEXT("StructMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : StructMetaData)
	{
		RegisterMetaData<FCSManagedTypeInfo, FCSStructMetaData>(this, MetaData, AllTypes, UCSScriptStruct::StaticClass());
	}

	const TArray<TSharedPtr<FJsonValue>>& EnumMetaData = JsonObject->GetArrayField(TEXT("EnumMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : EnumMetaData)
	{
		RegisterMetaData<FCSManagedTypeInfo, FCSEnumMetaData>(this, MetaData, AllTypes, UCSEnum::StaticClass());
	}

	const TArray<TSharedPtr<FJsonValue>>& InterfacesMetaData = JsonObject->GetArrayField(TEXT("InterfacesMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : InterfacesMetaData)
	{
		RegisterMetaData<FCSManagedTypeInfo, FCSInterfaceMetaData>(this, MetaData, AllTypes, UCSInterface::StaticClass());
	}

	const TArray<TSharedPtr<FJsonValue>>& DelegatesMetaData = JsonObject->GetArrayField(TEXT("DelegateMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : DelegatesMetaData)
	{
		RegisterMetaData<FCSManagedTypeInfo, FCSDelegateMetaData>(this, MetaData, AllTypes, UDelegateFunction::StaticClass());
	}

	const TArray<TSharedPtr<FJsonValue>>& ClassesMetaData = JsonObject->GetArrayField(TEXT("ClassMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : ClassesMetaData)
	{
		RegisterMetaData<FCSClassInfo, FCSClassMetaData>(this, MetaData, AllTypes, UCSClass::StaticClass(),
         [&Manager](const TSharedPtr<FCSManagedTypeInfo>& ClassInfo)
         {
             // Structure has been changed. We must trigger full reload on all managed classes that derive from this class.
             TArray<UClass*> DerivedClasses;
             GetDerivedClasses(ClassInfo->GetFieldChecked<UClass>(), DerivedClasses);

             for (UClass* DerivedClass : DerivedClasses)
             {
                 if (!Manager.IsManagedType(DerivedClass))
                 {
                     continue;
                 }

                 UCSClass* ManagedClass = static_cast<UCSClass*>(DerivedClass);
                 TSharedPtr<FCSClassInfo> ChildClassInfo = ManagedClass->GetManagedTypeInfo<FCSClassInfo>();
                 ChildClassInfo->SetStructureState(HasChangedStructure);
             }
         });
	}

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
	for (TSharedPtr<FGCHandle>& Handle : AllocatedManagedHandles)
	{
		Handle->Dispose(AssemblyHandle);
		Handle.Reset();
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

	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(TypeHandle, GCHandleType::WeakHandle);
	AllocatedManagedHandles.Add(AllocatedHandle);
	ManagedClassHandles.Add(FieldName, AllocatedHandle);
	return AllocatedHandle;
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

void UCSAssembly::AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSClassInfo* NewClass)
{
	TSet<FCSClassInfo*>& PendingClass = PendingClasses.FindOrAdd(ParentClass);
	PendingClass.Add(NewClass);
}

void UCSAssembly::OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason)
{
	if (InModuleChangeReason != EModuleChangeReason::ModuleLoaded)
	{
		return;
	}

	int32 NumPendingClasses = PendingClasses.Num();
	for (auto Itr = PendingClasses.CreateIterator(); Itr; ++Itr)
	{
		UClass* Class = Itr.Key().GetOwningClass();

		if (!Class)
		{
			// Class still not loaded from this module.
			continue;
		}

		for (FCSClassInfo* PendingClass : Itr.Value())
		{
			PendingClass->StartBuildingManagedType();
		}

		Itr.RemoveCurrent();
	}

#if WITH_EDITOR
	if (NumPendingClasses != PendingClasses.Num())
	{
		UCSManager::Get().OnProcessedPendingClassesEvent().Broadcast();
	}
#endif
}
