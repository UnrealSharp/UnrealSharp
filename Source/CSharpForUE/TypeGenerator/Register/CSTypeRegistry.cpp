#include "CSTypeRegistry.h"
#include "CSharpForUE/CSharpForUE.h"
#include "Misc/FileHelper.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonReader.h"
#include "TypeInfo/CSClassInfo.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"
#include "UnrealSharpUtilities/UnrealSharpStatics.h"

template<typename T>
void DeserializeMetaDataObjects(const TArray<TSharedPtr<FJsonValue>>& MetaDataArray, TMap<FName, T>& Map)
{
	for (const auto& MetaData : MetaDataArray)
	{
		T Info(MetaData);
		Map.Add(*Info.TypeMetaData->Name, Info);
	}
}

template<typename T>
void InitializeBuilders(TMap<FName, T>& Map)
{
	for (auto It = Map.CreateIterator(); It; ++It)
	{
		It->Value.InitializeBuilder();
	}
}

bool FCSTypeRegistry::ProcessMetaData(const FString& FilePath)
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

	DeserializeMetaDataObjects(JsonObject->GetArrayField("ClassMetaData"), ManagedClasses);
	DeserializeMetaDataObjects(JsonObject->GetArrayField("StructMetaData"), ManagedStructs);
	DeserializeMetaDataObjects(JsonObject->GetArrayField("EnumMetaData"), ManagedEnums);
	DeserializeMetaDataObjects(JsonObject->GetArrayField("InterfacesMetaData"), ManagedInterfaces);

	InitializeBuilders(ManagedClasses);
	InitializeBuilders(ManagedStructs);
	InitializeBuilders(ManagedEnums);
	InitializeBuilders(ManagedInterfaces);

	return true;
}

FCSharpClassInfo* FCSTypeRegistry::FindManagedType(UClass* Class)
{
	FCSharpClassInfo& ClassInfo = ManagedClasses.FindOrAdd(Class->GetFName());

	if (ClassInfo.Field == nullptr || ClassInfo.TypeHandle == nullptr)
	{
		const FString Namespace = UUnrealSharpStatics::GetNamespace(Class);
		ClassInfo.TypeHandle = FCSManager::Get().GetTypeHandle(FCSProcHelper::GetUserManagedProjectName(), Namespace, Class->GetName());
		ClassInfo.Field = Class;
	}

	return &ClassInfo;
}

void FCSTypeRegistry::AddPendingClass(FName ParentClass, FCSharpClassInfo* NewClass)
{
	FPendingClasses& PendingClass = PendingClasses.FindOrAdd(ParentClass);
	PendingClass.Classes.Add(NewClass);
}

UClass* FCSTypeRegistry::GetClassFromName(FName Name)
{
	UClass* FoundType;
	if (auto* TypeInfo = Get().ManagedClasses.Find(Name))
	{
		FoundType = TypeInfo->InitializeBuilder();
	}
	else
	{
		FoundType = FindObject<UClass>(ANY_PACKAGE, *Name.ToString());
	}
	
	return FoundType;
}

UScriptStruct* FCSTypeRegistry::GetStructFromName(FName Name)
{
	UScriptStruct* FoundType;
	if (auto* TypeInfo = Get().ManagedStructs.Find(Name))
	{
		FoundType = TypeInfo->InitializeBuilder();
	}
	else
	{
		FoundType = FindObject<UScriptStruct>(ANY_PACKAGE, *Name.ToString());
	}
	
	return FoundType;
}

UEnum* FCSTypeRegistry::GetEnumFromName(FName Name)
{
	UEnum* FoundType;
	if (auto* TypeInfo = Get().ManagedEnums.Find(Name))
	{
		FoundType = TypeInfo->InitializeBuilder();
	}
	else
	{
		FoundType = FindObject<UEnum>(ANY_PACKAGE, *Name.ToString());
	}
	
	return FoundType;
}

UClass* FCSTypeRegistry::GetInterfaceFromName(FName Name)
{
	UClass* FoundType;
	if (auto* TypeInfo = Get().ManagedInterfaces.Find(Name))
	{
		FoundType = TypeInfo->InitializeBuilder();
	}
	else
	{
		FoundType = FindObject<UClass>(ANY_PACKAGE, *Name.ToString());
	}
	
	return FoundType;
}

FCSharpClassInfo* FCSTypeRegistry::GetClassInfoFromName(FName Name)
{
	return Get().ManagedClasses.Find(Name);
}

FCSharpStructInfo* FCSTypeRegistry::GetStructInfoFromName(FName Name)
{
	return Get().ManagedStructs.Find(Name);
}

FCSharpEnumInfo* FCSTypeRegistry::GetEnumInfoFromName(FName Name)
{
	return Get().ManagedEnums.Find(Name);
}

FCSharpInterfaceInfo* FCSTypeRegistry::GetInterfaceInfoFromName(FName Name)
{
	return Get().ManagedInterfaces.Find(Name);
}

void FCSTypeRegistry::OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason)
{
	if (InModuleChangeReason != EModuleChangeReason::ModuleLoaded)
	{
		return;
	}
	
	for (auto Itr = PendingClasses.CreateIterator(); Itr; ++Itr)
	{
		UClass* Class = GetClassFromName(Itr.Key());
		
		if (!Class)
		{
			// Class still not loaded from this module.
			continue;
		}

		for (FCSharpClassInfo* PendingClass : Itr.Value().Classes)
		{
			PendingClass->InitializeBuilder();
		}

		Itr.RemoveCurrent();
	}
}
