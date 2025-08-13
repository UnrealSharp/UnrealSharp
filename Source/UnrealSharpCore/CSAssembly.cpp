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

	if (ProcessMetadata())
	{
		BuildUnrealTypes();
	}

	bIsLoading = false;
	UCSManager::Get().OnManagedAssemblyLoadedEvent().Broadcast(AssemblyName);
	return true;
}

template <typename T, typename MetaDataType>
void RegisterMetaData(UCSAssembly* OwningAssembly, const TSharedPtr<FJsonValue>& MetaData,
	TMap<FCSFieldName,
	TSharedPtr<T>>& Map,
	UClass* FieldType,
	TFunction<void(TSharedPtr<T>)> OnRebuild = nullptr)
{
	const TSharedPtr<FJsonObject>& MetaDataObject = MetaData->AsObject();

	const FString Name= MetaDataObject->GetStringField(TEXT("Name"));
	const FString Namespace = MetaDataObject->GetStringField(TEXT("Namespace"));
	const FCSFieldName FullName(*Name, *Namespace);

	TSharedPtr<T> ExistingValue = Map.FindRef(FullName);

	if (ExistingValue.IsValid())
	{
		// Parse fresh metadata and update the existing info
		TSharedPtr<MetaDataType> NewMeta = MakeShared<MetaDataType>();
		NewMeta->SerializeFromJson(MetaDataObject);

		if (ExistingValue->GetState() == NeedRebuild || *NewMeta != *ExistingValue->GetTypeMetaData<MetaDataType>())
		{
			ExistingValue->SetTypeMetaData(NewMeta);
			ExistingValue->SetState(NeedRebuild);
			
			if (OnRebuild)
			{
				OnRebuild(ExistingValue);
			}
		}
		else
		{
			ExistingValue->SetState(NeedUpdate);
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

bool UCSAssembly::ProcessMetadata()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::ProcessMetadata);

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
		RegisterMetaData<FCSManagedTypeInfo, FCSStructMetaData>(this, MetaData, Structs, UCSScriptStruct::StaticClass());
	}

	const TArray<TSharedPtr<FJsonValue>>& EnumMetaData = JsonObject->GetArrayField(TEXT("EnumMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : EnumMetaData)
	{
		RegisterMetaData<FCSManagedTypeInfo, FCSEnumMetaData>(this, MetaData, Enums, UCSEnum::StaticClass());
	}

	const TArray<TSharedPtr<FJsonValue>>& InterfacesMetaData = JsonObject->GetArrayField(TEXT("InterfacesMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : InterfacesMetaData)
	{
		RegisterMetaData<FCSManagedTypeInfo, FCSInterfaceMetaData>(this, MetaData, Interfaces, UCSInterface::StaticClass());
	}

	const TArray<TSharedPtr<FJsonValue>>& DelegatesMetaData = JsonObject->GetArrayField(TEXT("DelegateMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : DelegatesMetaData)
	{
		RegisterMetaData<FCSManagedTypeInfo, FCSDelegateMetaData>(this, MetaData, Delegates, UDelegateFunction::StaticClass());
	}

	const TArray<TSharedPtr<FJsonValue>>& ClassesMetaData = JsonObject->GetArrayField(TEXT("ClassMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : ClassesMetaData)
	{
		RegisterMetaData<FCSClassInfo, FCSClassMetaData>(this, MetaData, Classes, UCSClass::StaticClass(),
         [&Manager](const TSharedPtr<FCSClassInfo>& ClassInfo)
         {
             // Structure has been changed. We must trigger full reload on all managed classes that derive from this class.
             TArray<UClass*> DerivedClasses;
             GetDerivedClasses(ClassInfo->GetField<UClass>(), DerivedClasses);

             for (UClass* DerivedClass : DerivedClasses)
             {
                 if (!Manager.IsManagedType(DerivedClass))
                 {
                     continue;
                 }

                 UCSClass* ManagedClass = static_cast<UCSClass*>(DerivedClass);
                 TSharedPtr<FCSClassInfo> ChildClassInfo = ManagedClass->GetTypeInfo<FCSClassInfo>();
                 ChildClassInfo->SetState(NeedRebuild);
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

	return UCSManager::Get().GetManagedPluginsCallbacks().UnloadPlugin(*AssemblyPath);
}

UPackage* UCSAssembly::GetPackage(const FCSNamespace Namespace)
{
	UPackage* FoundPackage;
	if (GetDefault<UCSUnrealSharpSettings>()->HasNamespaceSupport())
	{
		FoundPackage = UCSManager::Get().FindOrAddManagedPackage(Namespace);
	}
	else
	{
		FoundPackage = UCSManager::Get().GetGlobalManagedPackage();
	}

	return FoundPackage;
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

TSharedPtr<FCSClassInfo> UCSAssembly::FindOrAddClassInfo(UClass* Class)
{
	if (UCSClass* ManagedClass = FCSClassUtilities::GetFirstManagedClass(Class))
	{
		return ManagedClass->GetTypeInfo<FCSClassInfo>();
	}
	
	FCSFieldName FieldName(Class);
	return FindOrAddClassInfo(FieldName);
}

TSharedPtr<FCSClassInfo> UCSAssembly::FindOrAddClassInfo(const FCSFieldName& ClassName)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::FindOrAddClassInfo);

	TSharedPtr<FCSClassInfo>& ClassInfo = Classes.FindOrAdd(ClassName);

	// Native classes are populated on the go when they are needed for managed code execution.
	if (!ClassInfo.IsValid())
	{
		UClass* Class = TryFindField<UClass>(ClassName);

		if (!IsValid(Class))
		{
			UE_LOGFMT(LogUnrealSharp, Error, "Failed to find native class: {0}", *ClassName.GetName());
			return nullptr;
		}

		TSharedPtr<FGCHandle> TypeHandle = TryFindTypeHandle(Class);

		if (!TypeHandle.IsValid())
		{
			UE_LOGFMT(LogUnrealSharp, Error, "Failed to find type handle for native class: {0}", *ClassName.GetName());
			return nullptr;
		}

		ClassInfo = MakeShared<FCSClassInfo>(Class, this, TypeHandle);
	}

	return ClassInfo;
}

UClass* UCSAssembly::FindClass(const FCSFieldName& FieldName) const
{
	return FindFieldFromInfo<UClass, FCSClassInfo>(FieldName, Classes);
}

UScriptStruct* UCSAssembly::FindStruct(const FCSFieldName& StructName) const
{
	return FindFieldFromInfo<UScriptStruct, FCSManagedTypeInfo>(StructName, Structs);
}

UEnum* UCSAssembly::FindEnum(const FCSFieldName& EnumName) const
{
	return FindFieldFromInfo<UEnum, FCSManagedTypeInfo>(EnumName, Enums);
}

UClass* UCSAssembly::FindInterface(const FCSFieldName& InterfaceName) const
{
	return FindFieldFromInfo<UClass, FCSManagedTypeInfo>(InterfaceName, Interfaces);
}

UDelegateFunction* UCSAssembly::FindDelegate(const FCSFieldName& DelegateName) const
{
	return FindFieldFromInfo<UDelegateFunction, FCSManagedTypeInfo>(DelegateName, Delegates);
}

TSharedPtr<FGCHandle> UCSAssembly::CreateManagedObject(const UObject* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::CreateManagedObject);

	// Only managed/native classes have a C# counterpart.
	UClass* Class = FCSClassUtilities::GetFirstNonBlueprintClass(Object->GetClass());
	TSharedPtr<FCSClassInfo> ClassInfo = FindOrAddClassInfo(Class);
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetManagedTypeHandle();

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

TSharedPtr<FGCHandle> UCSAssembly::FindOrCreateManagedObjectWrapper(UObject* Object, UClass* Class)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::CreateManagedObjectWrapper);

	ICSManagedTypeInterface* ManagedType = Cast<ICSManagedTypeInterface>(Class);
	FCSFieldName FieldName = ManagedType != nullptr ? ManagedType->GetTypeMetaData()->FieldName : FCSFieldName(Class);
	TSharedPtr<FGCHandle> TypeHandle = ManagedType->GetOwningAssembly()->TryFindTypeHandle(FieldName);
	
	uint32 ObjectID = Object->GetUniqueID();
    TMap<uint32, TSharedPtr<FGCHandle>>& TypeMap = UCSManager::Get().ManagedInterfaceWrappers.FindOrAddByHash(ObjectID, ObjectID);
	uint32 TypeId = Class->GetUniqueID();
	if (TSharedPtr<FGCHandle>* Existing = TypeMap.Find(TypeId); Existing != nullptr)
	{
		return *Existing;
	}

    TSharedPtr<FGCHandle>* ObjectHandle = UCSManager::Get().ManagedObjectHandles.Find(ObjectID);
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
			PendingClass->InitializeBuilder();
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

template <typename T>
void InitializeBuilders(TMap<FCSFieldName, T>& Map)
{
	for (auto It = Map.CreateIterator(); It; ++It)
	{
		It->Value->InitializeBuilder();
	}
}

void UCSAssembly::BuildUnrealTypes()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::BuildUnrealTypes);

	InitializeBuilders(Structs);
	InitializeBuilders(Enums);
	InitializeBuilders(Classes);
	InitializeBuilders(Interfaces);
	InitializeBuilders(Delegates);
}
