#include "CSAssembly.h"
#include "CSManagedMethod.h"
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
#include "TypeGenerator/Register/TypeInfo/CSEnumInfo.h"
#include "TypeGenerator/Register/TypeInfo/CSInterfaceInfo.h"
#include "TypeGenerator/Register/TypeInfo/CSStructInfo.h"

FCSAssembly::FCSAssembly(const FString& InAssemblyPath)
{
	AssemblyPath = FPaths::ConvertRelativePathToFull(InAssemblyPath);

#if defined(_WIN32)
	// Replace forward slashes with backslashes
	AssemblyPath.ReplaceInline(TEXT("/"), TEXT("\\"));
#endif
		
	AssemblyName = *FPaths::GetBaseFilename(AssemblyPath);

	GUObjectArray.AddUObjectDeleteListener(this);

	FModuleManager::Get().OnModulesChanged().AddRaw(this, &FCSAssembly::OnModulesChanged);
	
	// Remove this listener when the engine is shutting down.
	// Otherwise, we'll get a crash when the GC cleans up all the UObject.
	FCoreDelegates::OnPreExit.AddRaw(this, &FCSAssembly::OnEnginePreExit);
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

template<typename T, typename MetaDataType>
void RegisterMetaData(TSharedPtr<FCSAssembly> OwningAssembly, const TSharedPtr<FJsonValue>& MetaData, TMap<FCSFieldName, TSharedPtr<T>>& Map, TFunction<void(TSharedPtr<T>)> OnRebuild = nullptr)
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
		RegisterMetaData<FCSharpStructInfo, FCSStructMetaData>(OwningAssembly, MetaData, Structs);
	}

	const TArray<TSharedPtr<FJsonValue>>& EnumMetaData = JsonObject->GetArrayField(TEXT("EnumMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : EnumMetaData)
	{
		RegisterMetaData<FCSharpEnumInfo, FCSEnumMetaData>(OwningAssembly, MetaData, Enums);
	}

	const TArray<TSharedPtr<FJsonValue>>& InterfacesMetaData = JsonObject->GetArrayField(TEXT("InterfacesMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : InterfacesMetaData)
	{
		RegisterMetaData<FCSharpInterfaceInfo, FCSInterfaceMetaData>(OwningAssembly, MetaData, Interfaces);
	}

	const TArray<TSharedPtr<FJsonValue>>& ClassesMetaData = JsonObject->GetArrayField(TEXT("ClassMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : ClassesMetaData)
	{
		RegisterMetaData<FCSharpClassInfo, FCSClassMetaData>(OwningAssembly, MetaData, Classes, [&Manager](const TSharedPtr<FCSharpClassInfo>& ClassInfo)
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
				TSharedPtr<FCSharpClassInfo> ChildClassInfo = ManagedClass->GetClassInfo();
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
	ManagedObjectHandles.Reset();
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
		FoundPackage = UCSManager::Get().FindManagedPackage(Namespace);
	}
	else
	{
		FoundPackage = UCSManager::Get().GetGlobalUnrealSharpPackage();
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
	
	if (TypeHandle == nullptr)
	{
		return nullptr;
	}
	
	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(TypeHandle, GCHandleType::WeakHandle);
	
	AllocatedManagedHandles.Add(AllocatedHandle);
	ManagedClassHandles.Add(FieldName, AllocatedHandle);
	
	return AllocatedHandle;
}

TSharedPtr<FGCHandle> FCSAssembly::TryFindTypeHandle(UClass* Class)
{
	return TryFindTypeHandle(FCSFieldName(Class));
}

FCSManagedMethod FCSAssembly::GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName)
{
	if (!TypeHandle.IsValid())
	{
		return FCSManagedMethod::Invalid();
	}
	
	uint8* MethodHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedMethod(TypeHandle->GetPointer(), *MethodName);
	
	if (MethodHandle == nullptr)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to find method %s", *MethodName);
		return FCSManagedMethod::Invalid();
	}
	
	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(MethodHandle, GCHandleType::WeakHandle);
	AllocatedManagedHandles.Add(AllocatedHandle);

	FCSManagedMethod ManagedMethod(AllocatedHandle);
	return ManagedMethod;
}

FCSManagedMethod FCSAssembly::GetManagedMethod(const UCSClass* Class, const FString& MethodName)
{
	if (!IsValidAssembly())
	{
		return FCSManagedMethod::Invalid();
	}

	TSharedPtr<FCSharpClassInfo> ClassInfo = Class->GetClassInfo();
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetManagedTypeHandle();
	return GetManagedMethod(TypeHandle, MethodName);
}

TSharedPtr<FCSharpClassInfo> FCSAssembly::FindOrAddClassInfo(UClass* Class)
{
	if (UCSClass* ManagedClass = FCSGeneratedClassBuilder::GetFirstManagedClass(Class))
	{
		return ManagedClass->GetClassInfo();
	}
	
	FCSFieldName FieldName(Class);
	return FindOrAddClassInfo(FieldName);
}

TSharedPtr<FCSharpClassInfo> FCSAssembly::FindOrAddClassInfo(const FCSFieldName& ClassName)
{
	TSharedPtr<FCSharpClassInfo> FoundClassInfo = Classes.FindRef(ClassName);

	// Native classes are populated on the go when they are needed for managed code execution.
	if (!FoundClassInfo.IsValid())
	{
		UClass* Class = TryFindField<UClass>(ClassName);

		if (!IsValid(Class))
		{
			UE_LOGFMT(LogUnrealSharp, Error, "Failed to find native class: {0}", *ClassName.GetName());
			return nullptr;
		}
		
		TSharedPtr<FGCHandle> TypeHandle = TryFindTypeHandle(Class);

		if (!TypeHandle.IsValid() || TypeHandle->IsNull())
		{
			UE_LOGFMT(LogUnrealSharp, Error, "Failed to find type handle for native class: {0}", *ClassName.GetName());
			return nullptr;
		}
		
		FoundClassInfo = MakeShared<FCSharpClassInfo>(Class, SharedThis(this), TypeHandle);
		Classes.Add(ClassName, FoundClassInfo);
	}

	return FoundClassInfo;
}

TSharedPtr<FCSharpClassInfo> FCSAssembly::FindClassInfo(const FCSFieldName& ClassName) const
{
	return Classes.FindRef(ClassName);
}

TSharedPtr<FCSharpStructInfo> FCSAssembly::FindStructInfo(const FCSFieldName& StructName) const
{
	return Structs.FindRef(StructName);
}

TSharedPtr<FCSharpEnumInfo> FCSAssembly::FindEnumInfo(const FCSFieldName& EnumName) const
{
	return Enums.FindRef(EnumName);
}

TSharedPtr<FCSharpInterfaceInfo> FCSAssembly::FindInterfaceInfo(const FCSFieldName& InterfaceName) const
{
	return Interfaces.FindRef(InterfaceName);
}

UClass* FCSAssembly::FindClass(const FCSFieldName& FieldName) const
{
	UClass* Class;
	if (TSharedPtr<FCSharpClassInfo> ClassInfo = Classes.FindRef(FieldName))
	{
		Class = ClassInfo->InitializeBuilder();
	}
	else
	{
		Class = TryFindField<UClass>(FieldName);
	}
	
	return Class;
}

UScriptStruct* FCSAssembly::FindStruct(const FCSFieldName& StructName) const
{
	UScriptStruct* Struct;
	if (TSharedPtr<FCSharpStructInfo> StructInfo = Structs.FindRef(StructName))
	{
		Struct = StructInfo->InitializeBuilder();
	}
	else
	{
		Struct = TryFindField<UScriptStruct>(StructName);
	}

	check(Struct);
	return Struct;
}

UEnum* FCSAssembly::FindEnum(const FCSFieldName& EnumName) const
{
	UEnum* Enum;
	if (TSharedPtr<FCSharpEnumInfo> EnumInfo = Enums.FindRef(EnumName))
	{
		Enum = EnumInfo->InitializeBuilder();
	}
	else
	{
		Enum = TryFindField<UEnum>(EnumName);
	}

	check(Enum);
	return Enum;
}

UClass* FCSAssembly::FindInterface(const FCSFieldName& InterfaceName) const
{
	UClass* Interface;
	if (TSharedPtr<FCSharpInterfaceInfo> InterfaceInfo = Interfaces.FindRef(InterfaceName))
	{
		Interface = InterfaceInfo->InitializeBuilder();
	}
	else
	{
		Interface = TryFindField<UClass>(InterfaceName);
	}

	check(Interface);
	return Interface;
}

TSharedPtr<FGCHandle> FCSAssembly::CreateManagedObject(UObject* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::CreateNewManagedObject);
	
	if (TSharedPtr<FGCHandle> FoundHandle = ManagedObjectHandles.FindRef(Object->GetUniqueID()))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Object {0} already has a managed counterpart", *Object->GetName());
		return FoundHandle;
	}

	// Only managed/native classes have a C# counterpart.
	UClass* Class = FCSGeneratedClassBuilder::GetFirstNonBlueprintClass(Object->GetClass());
	
	TSharedPtr<FCSharpClassInfo> ClassInfo = FindOrAddClassInfo(Class);
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetManagedTypeHandle();
	
	FGCHandle NewManagedObject = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeHandle->GetPointer());
	NewManagedObject.Type = GCHandleType::StrongHandle;

	if (NewManagedObject.IsNull())
	{
		// This should never happen. Potential issues: IL errors, typehandle is invalid.
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to create managed counterpart for {0}", *Object->GetName());
		return nullptr;
	}

	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(NewManagedObject);
	ManagedObjectHandles.Add(Object->GetUniqueID(), AllocatedHandle);
	AllocatedManagedHandles.Add(AllocatedHandle);
	
	return AllocatedHandle;
}

FGCHandle FCSAssembly::FindManagedObject(UObject* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::FindManagedObject);
	
	if (!IsValid(Object))
	{
		RemoveManagedObject(Object);
		return FGCHandle();
	}

	TSharedPtr<FGCHandle> Handle = ManagedObjectHandles.FindRef(Object->GetUniqueID());
	
	if (!Handle.IsValid())
	{
		Handle = CreateManagedObject(Object);
	}
	
	return *Handle;
}

void FCSAssembly::AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSharpClassInfo* NewClass)
{
	TSet<FCSharpClassInfo*>& PendingClass = PendingClasses.FindOrAdd(ParentClass);
	PendingClass.Add(NewClass);
}

void FCSAssembly::RemoveManagedObject(const UObjectBase* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssembly::RemoveManagedObject);
	
	TSharedPtr<FGCHandle> ObjectHandle;
	if (ManagedObjectHandles.RemoveAndCopyValue(Object->GetUniqueID(), ObjectHandle))
	{
		FGCHandleIntPtr AssemblyHandle = ManagedAssemblyHandle->GetHandle();
		ObjectHandle->Dispose(AssemblyHandle);
	}
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

		for (FCSharpClassInfo* PendingClass : Itr.Value())
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

void FCSAssembly::OnEnginePreExit()
{
	GUObjectArray.RemoveUObjectDeleteListener(this);
}

void FCSAssembly::NotifyUObjectDeleted(const UObjectBase* Object, int32 Index)
{
	RemoveManagedObject(Object);
}

void FCSAssembly::OnUObjectArrayShutdown()
{
	GUObjectArray.RemoveUObjectDeleteListener(this);
}

template<typename T>
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
}





