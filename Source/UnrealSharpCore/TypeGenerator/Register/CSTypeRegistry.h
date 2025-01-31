#pragma once

#include "Dom/JsonValue.h"
#include "TypeInfo/CSClassInfo.h"
#include "TypeInfo/CSEnumInfo.h"
#include "TypeInfo/CSInterfaceInfo.h"
#include "TypeInfo/CSStructInfo.h"

DECLARE_MULTICAST_DELEGATE_OneParam(FOnNewClass, UClass*);
DECLARE_MULTICAST_DELEGATE_OneParam(FOnNewStruct, UScriptStruct*);
DECLARE_MULTICAST_DELEGATE_OneParam(PFOnNewEnum, UEnum*);

DECLARE_MULTICAST_DELEGATE(FOnPendingClassesProcessed);

struct FPendingClasses
{
	TSet<FCSharpClassInfo*> Classes;
};

class UNREALSHARPCORE_API FCSTypeRegistry
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
	FOnNewClass& GetOnNewInterfaceEvent() { return OnNewInterface; }
	FOnPendingClassesProcessed& GetOnPendingClassesProcessedEvent() { return OnPendingClassesProcessed; }

	static UClass* GetClassFromName(FName Name);
	static UScriptStruct* GetStructFromName(FName Name);
	static UEnum* GetEnumFromName(FName Name);
	static UClass* GetInterfaceFromName(FName Name);

	void RegisterClassToFilePath(const UTF16CHAR* ClassName, const UTF16CHAR* FilePath);
	void GetClassFilePath(FName ClassName, FString& OutFilePath);

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
	TMap<FName, FString> ClassToFilePath;
	
	FOnNewClass OnNewClass;
	FOnNewClass OnNewInterface;
	FOnNewStruct OnNewStruct;
	FOnPendingClassesProcessed OnPendingClassesProcessed;
	
};