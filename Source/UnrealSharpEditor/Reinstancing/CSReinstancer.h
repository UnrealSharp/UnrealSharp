#pragma once

class FCSReload;
class UClass;

class FCSReinstancer final
{
public:
	
	// Access the singleton instance
	static FCSReinstancer& Get();

	void Initialize();

	// Add a pending pair of classes to be re-instanced
	void AddPendingClass(UClass* OldClass, UClass* NewClass);

	// Add a pending pair of structs to be re-instanced
	void AddPendingStruct(UScriptStruct* OldStruct, UScriptStruct* NewStruct);

	// Add a pending pair of structs to be re-instanced
	void AddPendingEnum(UEnum* OldEnum, UEnum* NewEnum);

	// Add a pending pair of structs to be re-instanced
	void AddPendingInterface(UClass* OldInterface, UClass* NewInterface);

	// Process any pending re-instance requests
	void StartReinstancing();

	void GetDependentBlueprints(TArray<UBlueprint*>& DependentBlueprints);

	void PostReinstance();

	static void GetTablesDependentOnStruct(UScriptStruct* Struct, TArray<UDataTable*>& DataTables);

	friend FCSReload;

private:
	
	// Pending classes/interfaces to reinstance
	TMap<UClass*, UClass*> ClassesToReinstance;

	// Pending structs to reinstance
	TMap<UScriptStruct*, UScriptStruct*> StructsToReinstance;

	// Pending enums to reinstance
	TMap<UEnum*, UEnum*> EnumsToReinstance;

	// Pending interfaces to reinstance
	TMap<UClass*, UClass*> InterfacesToReinstance;
	
};
