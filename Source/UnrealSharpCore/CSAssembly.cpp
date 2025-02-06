#include "CSAssembly.h"
#include "UnrealSharpCore.h"
#include "Misc/Paths.h"
#include "CSManager.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "TypeGenerator/Register/MetaData/CSEnumMetaData.h"
#include "TypeGenerator/Register/MetaData/CSInterfaceMetaData.h"
#include "TypeGenerator/Register/MetaData/CSStructMetaData.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

FCSAssembly::FCSAssembly(const FString& InAssemblyPath)
{
	AssemblyPath = FPaths::ConvertRelativePathToFull(InAssemblyPath);

#if defined(_WIN32)
	// Replace forward slashes with backslashes
	AssemblyPath.ReplaceInline(TEXT("/"), TEXT("\\"));
#endif
		
	AssemblyName = *FPaths::GetBaseFilename(AssemblyPath);
}

bool FCSAssembly::Load(bool bProcessMetaData)
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
	
	Assembly.Handle = UCSManager::Get().GetManagedPluginsCallbacks().LoadPlugin(*AssemblyPath);
	Assembly.Type = GCHandleType::WeakHandle;

	if (!IsValid())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load: %s"), *AssemblyPath);
		return false;
	}

	if (bProcessMetaData)
	{
		// Change from ManagedProjectName.dll > ManagedProjectName.metadata.json
		const FString MetadataPath = FPaths::ChangeExtension(AssemblyPath, "metadata.json");
		return ProcessMetaData(MetadataPath);
	}
	
	return true;
}

bool FCSAssembly::Unload()
{
	for (const TSharedPtr<FGCHandle>& Handle : AllocatedHandles)
	{
		Handle->Dispose();
	}

	AllocatedHandles.Empty();
	
	if (!UCSManager::Get().GetManagedPluginsCallbacks().UnloadPlugin(*AssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Failed to unload: %s"), *AssemblyPath);
		return false;
	}
	
	return true;
}

#if WITH_EDITOR
bool FCSAssembly::Reload()
{
	return Unload() && Load();
}
#endif

bool FCSAssembly::IsValid() const
{
	return !Assembly.IsNull();
}

TSharedPtr<FGCHandle> FCSAssembly::GetTypeHandle(const FString& Namespace, const FString& TypeName)
{
	if (!IsValid())
	{
		return nullptr;
	}
	
	uint8* TypeHandle = UCSManager::Get().GetTypeHandle(Assembly.GetPointer(), Namespace, TypeName);
	
	if (TypeHandle == nullptr)
	{
		return nullptr;
	}
	
	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(TypeHandle, GCHandleType::WeakHandle);
	AllocatedHandles.Add(AllocatedHandle);
	return AllocatedHandle;
}

TSharedPtr<FGCHandle> FCSAssembly::GetTypeHandle(const UClass* Class)
{
	return GetTypeHandle(FUnrealSharpUtils::GetNamespace(Class), Class->GetName());
}

TSharedPtr<FGCHandle> FCSAssembly::GetMethodHandle(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName)
{
	if (!IsValid())
	{
		return nullptr;
	}
	
	uint8* MethodHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedMethod(TypeHandle->GetPointer(), *MethodName);
	
	if (MethodHandle == nullptr)
	{
		return nullptr;
	}
	
	TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(MethodHandle, GCHandleType::WeakHandle);
	AllocatedHandles.Add(AllocatedHandle);
	return AllocatedHandle;
}

TSharedPtr<FGCHandle> FCSAssembly::GetMethodHandle(const UCSClass* Class, const FString& MethodName)
{
	return GetMethodHandle(Class->GetClassInfo()->TypeHandle, MethodName);
}

TSharedPtr<FCSharpClassInfo> FCSAssembly::FindOrAddClassInfo(UClass* Class)
{
	FString Name = Class->GetName();
	Name.RemoveFromEnd(TEXT("_C"));
	
	TSharedPtr<FCSharpClassInfo> FoundClassInfo = ManagedClasses.FindRef(*Name);

	// Native classes are populated on the go as they are needed for managed code.
	if (!FoundClassInfo.IsValid())
	{
		FoundClassInfo = MakeShared<FCSharpClassInfo>(Class);
		ManagedClasses.Add(Class->GetFName(), FoundClassInfo);
	}
	else
	{
		FoundClassInfo->TryUpdateTypeHandle();
	}
	
	return FoundClassInfo.ToSharedRef();
}

TSharedPtr<FCSharpClassInfo> FCSAssembly::FindClassInfo(FName ClassName) const
{
	return ManagedClasses.FindRef(ClassName);
}

FGCHandle* FCSAssembly::CreateNewManagedObject(UObject* Object)
{
	ensureAlways(!UnmanagedToManagedMap.Contains(Object));
	
	TSharedPtr<FCSharpClassInfo> ClassInfo = FindOrAddClassInfo(Object->GetClass());
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->TypeHandle;
	
	FGCHandle NewManagedObject = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeHandle->GetPointer());
	NewManagedObject.Type = GCHandleType::StrongHandle;

	if (NewManagedObject.IsNull())
	{
		// This should never happen. Potential issues: IL errors, typehandle is invalid.
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to create managed object for %s"), *Object->GetName());
		return nullptr;
	}
	
	return &UnmanagedToManagedMap.Add(Object, NewManagedObject);
}

FGCHandle* FCSAssembly::FindManagedObject(UObject* Object)
{
	if (!::IsValid(Object))
	{
		RemoveManagedObject(Object);
		return nullptr;
	}
	
	if (FGCHandle* Handle = UnmanagedToManagedMap.Find(Object))
	{
		return Handle;
	}

	return CreateNewManagedObject(Object);
}

void FCSAssembly::RemoveManagedObject(const UObjectBase* Object)
{
	FGCHandle Handle;
	if (UnmanagedToManagedMap.RemoveAndCopyValue(Object, Handle))
	{
		Handle.Dispose();
	}
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

bool FCSAssembly::ProcessMetaData(const FString& FilePath)
{
	if (!FPaths::FileExists(FilePath))
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Couldn't find metadata file at: %s"), *FilePath);
		return false;
	}

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
		RegisterMetaData<FCSharpStructInfo, FCSStructMetaData>(OwningAssembly, MetaData, ManagedStructs);
	}

	const TArray<TSharedPtr<FJsonValue>>& EnumMetaData = JsonObject->GetArrayField(TEXT("EnumMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : EnumMetaData)
	{
		RegisterMetaData<FCSharpEnumInfo, FCSEnumMetaData>(OwningAssembly, MetaData, ManagedEnums);
	}

	const TArray<TSharedPtr<FJsonValue>>& InterfacesMetaData = JsonObject->GetArrayField(TEXT("InterfacesMetaData"));
	for (const TSharedPtr<FJsonValue>& MetaData : InterfacesMetaData)
	{
		RegisterMetaData<FCSharpInterfaceInfo, FCSInterfaceMetaData>(OwningAssembly, MetaData, ManagedInterfaces);
	}
	
	for (const TSharedPtr<FJsonValue>& MetaData : JsonObject->GetArrayField(TEXT("ClassMetaData")))
	{
		RegisterMetaData<FCSharpClassInfo, FCSClassMetaData>(OwningAssembly, MetaData, ManagedClasses, [](const TSharedPtr<FCSharpClassInfo>& ClassInfo)
		{
			ClassInfo->State = ETypeState::NeedUpdate;
		});
	}

	InitializeBuilders(ManagedStructs);
	InitializeBuilders(ManagedEnums);
	InitializeBuilders(ManagedClasses);
	InitializeBuilders(ManagedInterfaces);
	
	return true;
}




