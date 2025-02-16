#include "CSAssembly.h"
#include "UnrealSharpCore.h"
#include "Misc/Paths.h"
#include "CSManager.h"
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
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

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
	if (IsValid())
	{
		UE_LOG(LogUnrealSharp, Display, TEXT("%s is already loaded"), *AssemblyPath);
		return true;
	}
	
	if (!FPaths::FileExists(AssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Display, TEXT("%s doesn't exist"), *AssemblyPath);
		return false;
	}
	
	Assembly.Handle = UCSManager::Get().GetManagedPluginsCallbacks().LoadPlugin(*AssemblyPath, bisCollectible);
	Assembly.Type = GCHandleType::WeakHandle;

	if (!IsValid())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load: %s"), *AssemblyPath);
		return false;
	}

	if (ProcessMetadata())
	{
		BuildUnrealTypes();
	}

	return true;
}

bool FCSAssembly::ProcessMetadata()
{
	const FString MetadataPath = FPaths::ChangeExtension(AssemblyPath, "metadata.json");
	if (!FPaths::FileExists(MetadataPath))
	{
		return true;
	}

	return ProcessMetaData_Internal(MetadataPath);
}

bool FCSAssembly::UnloadAssembly()
{
	ClassHandles.Empty();
	
	for (TSharedPtr<FGCHandle>& Handle : AllHandles)
	{
		Handle->Dispose();
		
		int32 RefCount = Handle.GetSharedReferenceCount();
		if (RefCount > 1)
		{
			UE_LOG(LogUnrealSharp, Error, TEXT("Handle %p has %d references"), Handle.Get(), RefCount);
			ensureAlwaysMsgf(false, TEXT("Handle has references"));
		}

		Handle.Reset();
	}
	
	AllHandles.Empty();
	Assembly.Dispose();
	
	if (!UCSManager::Get().GetManagedPluginsCallbacks().UnloadPlugin(*AssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Failed to unload: %s"), *AssemblyPath);
		return false;
	}
	
	return true;
}

UPackage* FCSAssembly::GetPackage(const FName Namespace)
{
	const UCSUnrealSharpSettings* Settings = GetDefault<UCSUnrealSharpSettings>();
	UCSManager& Manager = UCSManager::Get();

	UPackage* FoundPackage;
	if (Settings->bEnableNamespaceSupport)
	{
		for (UPackage* Package : AssemblyPackages)
		{
			if (Package->GetFName() == Namespace)
			{
				return Package;
			}
		}
		
		FoundPackage = Manager.CreateNewUnrealSharpPackage(Namespace.ToString());
	}
	else
	{
		FoundPackage = Manager.GetGlobalUnrealSharpPackage();
	}

	return FoundPackage;
}

bool FCSAssembly::ContainsClass(const UClass* Class) const
{
	return Classes.Contains(Class->GetFName());
}

TWeakPtr<FGCHandle> FCSAssembly::TryFindTypeHandle(const FName& Namespace, const FName& TypeName)
{
	if (!IsValid())
	{
		return nullptr;
	}

	if (TSharedPtr<FGCHandle>* Handle = ClassHandles.Find(TypeName))
	{
		return *Handle;
	}

	uint8* TypeHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedType(Assembly.GetPointer(), *Namespace.ToString(), *TypeName.ToString());
	
	if (TypeHandle == nullptr)
	{
		return nullptr;
	}
	
	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(TypeHandle, GCHandleType::WeakHandle);
	
	AllHandles.Add(AllocatedHandle);
	ClassHandles.Add(TypeName, AllocatedHandle);
	
	return AllocatedHandle.ToWeakPtr();
}

TWeakPtr<FGCHandle> FCSAssembly::TryFindTypeHandle(const UClass* Class)
{
	return TryFindTypeHandle(FUnrealSharpUtils::GetNamespace(Class), Class->GetFName());
}


TWeakPtr<FGCHandle> FCSAssembly::GetMethodHandle(const UCSClass* Class, const FString& MethodName)
{
	if (!IsValid())
	{
		return nullptr;
	}

	TSharedRef<const FCSharpClassInfo> ClassInfo = Class->GetClassInfo();
	TSharedPtr<FGCHandle> PinnedHandle = ClassInfo->GetTypeHandle().Pin();
	
	uint8* MethodHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedMethod(PinnedHandle->GetPointer(), *MethodName);
	
	if (MethodHandle == nullptr)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to find method {0} in {1}", *MethodName, *Class->GetName());
		return nullptr;
	}
	
	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(MethodHandle, GCHandleType::WeakHandle);
	AllHandles.Add(AllocatedHandle);
	return AllocatedHandle.ToWeakPtr();
}

TSharedPtr<FCSharpClassInfo> FCSAssembly::FindOrAddClassInfo(const UClass* Class)
{
	FString Name = Class->GetName();
	if (Class->HasAllClassFlags(CLASS_CompiledFromBlueprint))
	{
		Name.RemoveFromEnd(TEXT("_C"));
	}
	
	return FindOrAddClassInfo(*Name);
}

TSharedPtr<FCSharpClassInfo> FCSAssembly::FindOrAddClassInfo(FName ClassName)
{
	TSharedPtr<FCSharpClassInfo> FoundClassInfo = Classes.FindRef(ClassName);

	// Native classes are populated on the go as they are needed for managed code.
	if (!FoundClassInfo.IsValid())
	{
		UClass* Class = FindFirstObject<UClass>(*ClassName.ToString());
		TWeakPtr<FGCHandle> TypeHandle = TryFindTypeHandle(Class);
		FoundClassInfo = MakeShared<FCSharpClassInfo>(Class, SharedThis(this), TypeHandle);
		Classes.Add(ClassName, FoundClassInfo);
	}

	return FoundClassInfo;
}

TSharedPtr<FCSharpClassInfo> FCSAssembly::FindClassInfo(FName ClassName) const
{
	return Classes.FindRef(ClassName);
}

TSharedPtr<FCSharpStructInfo> FCSAssembly::FindStructInfo(FName StructName) const
{
	return Structs.FindRef(StructName);
}

TSharedPtr<FCSharpEnumInfo> FCSAssembly::FindEnumInfo(FName EnumName) const
{
	return Enums.FindRef(EnumName);
}

TSharedPtr<FCSharpInterfaceInfo> FCSAssembly::FindInterfaceInfo(FName InterfaceName) const
{
	return Interfaces.FindRef(InterfaceName);
}

UClass* FCSAssembly::FindClass(FName ClassName) const
{
	UClass* Class;
	if (TSharedPtr<FCSharpClassInfo> ClassInfo = Classes.FindRef(ClassName))
	{
		Class = ClassInfo->InitializeBuilder();
	}
	else
	{
		Class = FindFirstObject<UClass>(*ClassName.ToString());
	}

	check(Class);
	return Class;
}

UScriptStruct* FCSAssembly::FindStruct(FName StructName) const
{
	UScriptStruct* Struct;
	if (TSharedPtr<FCSharpStructInfo> StructInfo = Structs.FindRef(StructName))
	{
		Struct = StructInfo->InitializeBuilder();
	}
	else
	{
		Struct = FindFirstObject<UScriptStruct>(*StructName.ToString());
	}

	check(Struct);
	return Struct;
}

UEnum* FCSAssembly::FindEnum(FName EnumName) const
{
	UEnum* Enum;
	if (TSharedPtr<FCSharpEnumInfo> EnumInfo = Enums.FindRef(EnumName))
	{
		Enum = EnumInfo->InitializeBuilder();
	}
	else
	{
		Enum = FindFirstObject<UEnum>(*EnumName.ToString());
	}

	check(Enum);
	return Enum;
}

UClass* FCSAssembly::FindInterface(FName InterfaceName) const
{
	UClass* Interface;
	if (TSharedPtr<FCSharpInterfaceInfo> InterfaceInfo = Interfaces.FindRef(InterfaceName))
	{
		Interface = InterfaceInfo->InitializeBuilder();
	}
	else
	{
		Interface = FindFirstObject<UClass>(*InterfaceName.ToString());
	}

	check(Interface);
	return Interface;
}


FGCHandle* FCSAssembly::CreateNewManagedObject(UObject* Object)
{
	ensureAlways(!ObjectHandles.Contains(Object));

	UClass* Class = FCSGeneratedClassBuilder::GetFirstManagedClass(Object->GetClass());
	if (!Class)
	{
		Class = FCSGeneratedClassBuilder::GetFirstNativeClass(Object->GetClass());
	}
	
	TSharedPtr<FCSharpClassInfo> ClassInfo = FindOrAddClassInfo(Class);
	TSharedPtr<const FGCHandle> TypeHandle = ClassInfo->GetTypeHandle().Pin();
	
	FGCHandle NewManagedObject = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeHandle->GetPointer());
	NewManagedObject.Type = GCHandleType::StrongHandle;

	if (NewManagedObject.IsNull())
	{
		// This should never happen. Potential issues: IL errors, typehandle is invalid.
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to create managed object for %s"), *Object->GetName());
		return nullptr;
	}
	
	return &ObjectHandles.Add(Object, NewManagedObject);
}

FGCHandle FCSAssembly::FindManagedObject(UObject* Object)
{
	if (!::IsValid(Object))
	{
		RemoveManagedObject(Object);
		return FGCHandle();
	}

	FGCHandle* Handle = ObjectHandles.Find(Object);
	if (!Handle)
	{
		Handle = CreateNewManagedObject(Object);
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
	FGCHandle Handle;
	if (ObjectHandles.RemoveAndCopyValue(Object, Handle))
	{
		Handle.Dispose();
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

void FCSAssembly::NotifyUObjectDeleted(const class UObjectBase* Object, int32 Index)
{
	RemoveManagedObject(Object);
}

void FCSAssembly::OnUObjectArrayShutdown()
{
	GUObjectArray.RemoveUObjectDeleteListener(this);
}

template<typename T>
void InitializeBuilders(TMap<FName, T>& Map)
{
	for (auto It = Map.CreateIterator(); It; ++It)
	{
		It->Value->InitializeBuilder();
	}
}

template<typename T, typename MetaDataType>
void RegisterMetaData(TSharedPtr<FCSAssembly> OwningAssembly, const TSharedPtr<FJsonValue>& MetaData, TMap<FName, TSharedPtr<T>>& Map, TFunction<void(TSharedPtr<T>)> OnUpdate = nullptr)
{
	const TSharedPtr<FJsonObject>& MetaDataObject = MetaData->AsObject();
	FString Name = MetaDataObject->GetStringField(TEXT("Name"));
	TSharedPtr<T> ExistingValue = Map.FindRef(*Name);
	
	if (ExistingValue.IsValid())
	{
		MetaDataType NewMetaData;
		NewMetaData.SerializeFromJson(MetaDataObject);

		if (NewMetaData != *ExistingValue->TypeMetaData)
		{
			*ExistingValue->TypeMetaData = NewMetaData;
			ExistingValue->State = NeedRebuild;
		}
		else if (OnUpdate)
		{
			OnUpdate(ExistingValue);
		}
	}
	else
	{
		ExistingValue = MakeShared<T>(MetaData, OwningAssembly);
		Map.Add(*Name, ExistingValue);
	}
}

bool FCSAssembly::ProcessMetaData_Internal(const FString& FilePath)
{
	FString JsonString;
	if (!FFileHelper::LoadFileToString(JsonString, *FilePath))
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load MetaDataPath at: %s"), *FilePath);
		return false;
	}

	TSharedPtr<FJsonObject> JsonObject;
	if (!FJsonSerializer::Deserialize(TJsonReaderFactory<>::Create(JsonString), JsonObject) || !JsonObject.IsValid())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to parse JSON at: %s"), *FilePath);
		return false;
	}

	TSharedPtr<FCSAssembly> OwningAssembly = SharedThis(this);

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
	
	for (const TSharedPtr<FJsonValue>& MetaData : JsonObject->GetArrayField(TEXT("ClassMetaData")))
	{
		RegisterMetaData<FCSharpClassInfo, FCSClassMetaData>(OwningAssembly, MetaData, Classes, [OwningAssembly](const TSharedPtr<FCSharpClassInfo>& ClassInfo)
		{
			ClassInfo->State = ETypeState::NeedUpdate;
			ClassInfo->TypeHandle = OwningAssembly->TryFindTypeHandle(ClassInfo->TypeMetaData->Namespace, ClassInfo->TypeMetaData->Name);
		});
	}
	
	return true;
}

void FCSAssembly::BuildUnrealTypes()
{
	InitializeBuilders(Structs);
	InitializeBuilders(Enums);
	InitializeBuilders(Classes);
	InitializeBuilders(Interfaces);
}





