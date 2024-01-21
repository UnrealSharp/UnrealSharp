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

	FCSharpClassInfo* FindManagedType(UClass* Class);
	void AddPendingClass(FName ParentClass, FCSharpClassInfo* NewClass);

	FOnNewClass& GetOnNewClassEvent() { return OnNewClass; }
	FOnNewStruct& GetOnNewStructEvent() { return OnNewStruct; }
	FOnNewEnum& GetOnNewEnumEvent() { return OnNewEnum; }

	static UClass* GetClassFromName(FName Name);
	static UScriptStruct* GetStructFromName(FName Name);
	static UEnum* GetEnumFromName(FName Name);
	static UClass* GetInterfaceFromName(FName Name);

	static FCSharpClassInfo* GetClassInfoFromName(FName Name);
	static FCSharpStructInfo* GetStructInfoFromName(FName Name);
	static FCSharpEnumInfo* GetEnumInfoFromName(FName Name);
	static FCSharpInterfaceInfo* GetInterfaceInfoFromName(FName Name);

	TMap<FName, FCSharpClassInfo> ManagedClasses;
	TMap<FName, FCSharpStructInfo> ManagedStructs;
	TMap<FName, FCSharpEnumInfo> ManagedEnums;
	TMap<FName, FCSharpInterfaceInfo> ManagedInterfaces;

private:
	
	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	
	TMap<FName, FPendingClasses> PendingClasses;
	
	FOnNewClass OnNewClass;
	FOnNewStruct OnNewStruct;
	FOnNewEnum OnNewEnum;
	
};