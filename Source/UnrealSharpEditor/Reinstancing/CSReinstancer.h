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
	void Reinstance();

	friend FCSReload;

private:
	
	// Pending classes/interfaces to reinstance
	TArray<TPair<TObjectPtr<UClass>, TObjectPtr<UClass>>> ClassesToReinstance;

	// Pending structs to reinstance
	TArray<TPair<TObjectPtr<UScriptStruct>, TObjectPtr<UScriptStruct>>> StructsToReinstance;

	// Pending enums to reinstance
	TArray<TPair<TObjectPtr<UEnum>, TObjectPtr<UEnum>>> EnumsToReinstance;

	// Pending interfaces to reinstance
	TArray<TPair<TObjectPtr<UClass>, TObjectPtr<UClass>>> InterfacesToReinstance;
	
};
