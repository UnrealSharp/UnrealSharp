#include "CSAssembly.h"
#include "UnrealSharpCore.h"
#include "Misc/Paths.h"
#include "CSManager.h"
#include "CSUnrealSharpSettings.h"
#include "Logging/StructuredLog.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "TypeGenerator/Register/MetaData/CSEnumMetaData.h"
#include "TypeGenerator/Register/MetaData/CSInterfaceMetaData.h"
#include "TypeGenerator/Register/MetaData/CSStructMetaData.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"
#include "TypeGenerator/Register/TypeInfo/CSDelegateInfo.h"
#include "TypeGenerator/Register/TypeInfo/CSEnumInfo.h"
#include "TypeGenerator/Register/TypeInfo/CSInterfaceInfo.h"
#include "TypeGenerator/Register/TypeInfo/CSStructInfo.h"
#include "Utils/CSClassUtilities.h"

FCSAssembly::FCSAssembly(const FString& InAssemblyPath)
{
	AssemblyPath = FPaths::ConvertRelativePathToFull(InAssemblyPath);

#if defined(_WIN32)
	// Replace forward slashes with backslashes
	AssemblyPath.ReplaceInline(TEXT("/"), TEXT("\\"));
#endif

	AssemblyName = *FPaths::GetBaseFilename(AssemblyPath);

	FModuleManager::Get().OnModulesChanged().AddRaw(this, &FCSAssembly::OnModulesChanged);
}

bool FCSAssembly::LoadAssembly(bool bisCollectible)
{
	TRACE_CPUPROFILER_EVENT_SCOPE_TEXT(*FString(TEXT("FCSAssembly::LoadAssembly: " + AssemblyName.ToString())));

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

	if (ProcessMetadata())
	{
		BuildUnrealTypes();
	}

	bIsLoading = false;
	UCSManager::Get().OnManagedAssemblyLoadedEvent().Broadcast(AssemblyName);
	return true;
}

template <typename T, typename MetaDataType>
void RegisterMetaData(TSharedPtr<FCSAssembly> OwningAssembly, const TSharedPtr<FJsonValue>& MetaData,
                      TMap<FCSFieldName, TSharedPtr<T>>& Map, TFunction<void(TSharedPtr<T>)> OnRebuild = nullptr)
{
	const TSharedPtr<FJsonObject>& MetaDataObject = MetaData->AsObject();

	FString Name = MetaDataObject->GetStringField(TEXT("Name"));
	FString Namespace = MetaDataObject->GetStringField(TEXT("Namespace"));
	FCSFieldName FullName(*Name, *Namespace);

	TSharedPtr<T> ExistingValue = Map.FindRef(FullName);

	if (ExistingValue.IsValid())
	{
		MetaDataType NewMetaData;
		NewMetaData.SerializeFromJson(MetaDataObject);

		if (ExistingValue->State == NeedRebuild || NewMetaData != *ExistingValue->TypeMetaData)
		{
			*ExistingValue->TypeMetaData = NewMetaData;
			ExistingValue->State = NeedRebuild;

			if (OnRebuild)
			{
				OnRebuild(ExistingValue);
			}
		}
		else
		{
			ExistingValue->State = NeedUpdate;
		}
	}
	else
	{
		ExistingValue = MakeShared<T>(MetaData, OwningAssembly);
		Map.Add(FullName, ExistingValue);
	}
}

bool FCSAssembly::ProcessMetadata()
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

	TSharedPtr<FCSAssembly> OwningAssembly = SharedThis(this);
	UCSManager& Manager = UCSManager::Get();

	const TArray<TSharedPtr<FJsonValue>>& StructMetaData = JsonObject->GetArrayField(TEXT("StructMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : StructMetaData)
	{
		RegisterMetaData<FCSStructInfo, FCSStructMetaData>(OwningAssembly, MetaData, Structs);
	}

	const TArray<TSharedPtr<FJsonValue>>& EnumMetaData = JsonObject->GetArrayField(TEXT("EnumMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : EnumMetaData)
	{
		RegisterMetaData<FCSEnumInfo, FCSEnumMetaData>(OwningAssembly, MetaData, Enums);
	}

	const TArray<TSharedPtr<FJsonValue>>& InterfacesMetaData = JsonObject->GetArrayField(TEXT("InterfacesMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : InterfacesMetaData)
	{
		RegisterMetaData<FCSInterfaceInfo, FCSInterfaceMetaData>(OwningAssembly, MetaData, Interfaces);
	}

	const TArray<TSharedPtr<FJsonValue>>& DelegatesMetaData = JsonObject->GetArrayField(TEXT("DelegateMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : DelegatesMetaData)
	{
		RegisterMetaData<FCSDelegateInfo, FCSDelegateMetaData>(OwningAssembly, MetaData, Delegates);
	}

	const TArray<TSharedPtr<FJsonValue>>& ClassesMetaData = JsonObject->GetArrayField(TEXT("ClassMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : ClassesMetaData)
	{
		RegisterMetaData<FCSClassInfo, FCSClassMetaData>(OwningAssembly, MetaData, Classes,
         [&Manager](const TSharedPtr<FCSClassInfo>& ClassInfo)
         {
             // Structure has been changed. We must trigger full reload on all managed classes that derive from this class.
             TArray<UClass*> DerivedClasses;
             GetDerivedClasses(ClassInfo->Field, DerivedClasses);

             for (UClass* DerivedClass : DerivedClasses)
             {
                 if (!Manager.IsManagedField(DerivedClass))
                 {
                     continue;
                 }

                 UCSClass* ManagedClass = static_cast<UCSClass*>(DerivedClass);
                 TSharedPtr<FCSClassInfo> ChildClassInfo = ManagedClass->GetTypeInfo();
                 ChildClassInfo->State = NeedRebuild;
             }
         });
	}

	return true;
}

bool FCSAssembly::UnloadAssembly()
{
	if (!IsValidAssembly())
	{
		// Assembly is already unloaded.
		UE_LOGFMT(LogUnrealSharp, Display, "{0} is already unloaded", *AssemblyName.ToString());
		return true;
	}

	TRACE_CPUPROFILER_EVENT_SCOPE_TEXT(*FString(TEXT("FCSAssembly::UnloadAssembly: " + AssemblyName.ToString())));

	FGCHandleIntPtr AssemblyHandle = ManagedAssemblyHandle->GetHandle();
	for (TSharedPtr<FGCHandle>& Handle : AllocatedManagedHandles)
	{
		Handle->Dispose(AssemblyHandle);
		Handle.Reset();
	}

	ManagedClassHandles.Reset();
	AllocatedManagedHandles.Reset();

	// Don't need the assembly handle anymore, we use the path to unload the assembly.
	ManagedAssemblyHandle->Dispose();
	ManagedAssemblyHandle.Reset();

	return UCSManager::Get().GetManagedPluginsCallbacks().UnloadPlugin(*AssemblyPath);
}

UPackage* FCSAssembly::GetPackage(const FCSNamespace Namespace)
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

TSharedPtr<FGCHandle> FCSAssembly::TryFindTypeHandle(const FCSFieldName& FieldName)
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

TSharedPtr<FGCHandle> FCSAssembly::GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName)
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

TSharedPtr<FCSClassInfo> FCSAssembly::FindOrAddClassInfo(const FCSFieldName& ClassName)
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

		ClassInfo = MakeShared<FCSClassInfo>(Class, SharedThis(this), TypeHandle);
	}

	return ClassInfo;
}

UClass* FCSAssembly::FindClass(const FCSFieldName& FieldName) const
{
	return FindFieldFromInfo<UClass, FCSClassInfo>(FieldName, Classes);
}

UScriptStruct* FCSAssembly::FindStruct(const FCSFieldName& StructName) const
{
	return FindFieldFromInfo<UScriptStruct, FCSStructInfo>(StructName, Structs);
}

UEnum* FCSAssembly::FindEnum(const FCSFieldName& EnumName) const
{
	return FindFieldFromInfo<UEnum, FCSEnumInfo>(EnumName, Enums);
}

UClass* FCSAssembly::FindInterface(const FCSFieldName& InterfaceName) const
{
	return FindFieldFromInfo<UClass, FCSInterfaceInfo>(InterfaceName, Interfaces);
}

UDelegateFunction* FCSAssembly::FindDelegate(const FCSFieldName& DelegateName) const
{
	return FindFieldFromInfo<UDelegateFunction, FCSDelegateInfo>(DelegateName, Delegates);
}

TSharedPtr<FGCHandle> FCSAssembly::CreateManagedObject(UObject* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::CreateManagedObject);

	// Only managed/native classes have a C# counterpart.
	UClass* Class = FCSClassUtilities::GetFirstNonBlueprintClass(Object->GetClass());
	TSharedPtr<FCSClassInfo> ClassInfo = FindOrAddClassInfo(Class);
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetManagedTypeHandle();

	FGCHandle NewManagedObject = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeHandle->GetPointer());
	NewManagedObject.Type = GCHandleType::StrongHandle;

	if (NewManagedObject.IsNull())
	{
		// This should never happen. Potential issues: IL errors, typehandle is invalid.
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to create managed counterpart for {0}", *Object->GetName());
		return nullptr;
	}

	TSharedPtr<FGCHandle> Handle = MakeShared<FGCHandle>(NewManagedObject);
	AllocatedManagedHandles.Add(Handle);

	uint32 ObjectID = Object->GetUniqueID();
	UCSManager::Get().ManagedObjectHandles.AddByHash(ObjectID, ObjectID, Handle);

	return Handle;
}

void FCSAssembly::AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSClassInfo* NewClass)
{
	TSet<FCSClassInfo*>& PendingClass = PendingClasses.FindOrAdd(ParentClass);
	PendingClass.Add(NewClass);
}

void FCSAssembly::OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason)
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

void FCSAssembly::BuildUnrealTypes()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::BuildUnrealTypes);

	InitializeBuilders(Structs);
	InitializeBuilders(Enums);
	InitializeBuilders(Classes);
	InitializeBuilders(Interfaces);
	InitializeBuilders(Delegates);
}
