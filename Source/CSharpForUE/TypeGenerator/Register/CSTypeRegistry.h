#pragma once

#include "Dom/JsonValue.h"
#include "TypeInfo/CSClassInfo.h"
#include "TypeInfo/CSEnumInfo.h"
#include "TypeInfo/CSInterfaceInfo.h"
#include "TypeInfo/CSStructInfo.h"

DECLARE_MULTICAST_DELEGATE_TwoParams(FOnNewClass, UClass*, UClass*);
DECLARE_MULTICAST_DELEGATE_TwoParams(FOnNewStruct, UScriptStruct*, UScriptStruct*);
DECLARE_MULTICAST_DELEGATE_TwoParams(FOnNewEnum, UEnum*, UEnum*);

struct FPendingClasses
{
	TSet<FCSharpClassInfo*> Classes;
};

class CSHARPFORUE_API FCSTypeRegistry
{

public:

	FCSTypeRegistry()
	{
		FModuleManager::Get().OnModulesChanged().AddRaw(this, &FCSTypeRegistry::OnModulesChanged);
	}
	
	static FCSTypeRegistry& Get()
	{
		static FCSTypeRegistry Instance;
		return Instance;
	}

	bool ProcessMetaData(const FString& FilePath);

	TSharedRef<FCSharpClassInfo> FindManagedType(UClass* Class);
	void AddPendingClass(FName ParentClass, FCSharpClassInfo* NewClass);

	FOnNewClass& GetOnNewClassEvent() { return OnNewClass; }
	FOnNewStruct& GetOnNewStructEvent() { return OnNewStruct; }

	static UClass* GetClassFromName(FName Name);
	static UScriptStruct* GetStructFromName(FName Name);
	static UEnum* GetEnumFromName(FName Name);
	static UClass* GetInterfaceFromName(FName Name);

	static TSharedPtr<FCSharpClassInfo> GetClassInfoFromName(FName Name)
	{
		return Get().ManagedClasses.FindRef(Name);
	};
	
	static TSharedPtr<FCSharpStructInfo> GetStructInfoFromName(FName Name)
	{
		return Get().ManagedStructs.FindRef(Name);
	};
	
	static TSharedPtr<FCSharpEnumInfo> GetEnumInfoFromName(FName Name)
	{
		return Get().ManagedEnums.FindRef(Name);
	};
	
	static TSharedPtr<FCSharpInterfaceInfo> GetInterfaceInfoFromName(FName Name)
	{
		return Get().ManagedInterfaces.FindRef(Name);
	};

	TMap<FName, TSharedPtr<FCSharpClassInfo>> ManagedClasses;
	TMap<FName, TSharedPtr<FCSharpStructInfo>> ManagedStructs;
	TMap<FName, TSharedPtr<FCSharpEnumInfo>> ManagedEnums;
	TMap<FName, TSharedPtr<FCSharpInterfaceInfo>> ManagedInterfaces;

private:
	
	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	
	TMap<FName, FPendingClasses> PendingClasses;
	
	FOnNewClass OnNewClass;
	FOnNewStruct OnNewStruct;
	
};