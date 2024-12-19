#pragma once

class UK2Node_CSAsyncAction;
class UK2Node_CallFunction;
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
	void AddPendingInterface(UClass* OldInterface, UClass* NewInterface);

	// Process any pending re-instance requests
	void StartReinstancing();

	void PostReinstance();

	void FixDataTables();

	void UpdateBlueprints();
	
	bool TryUpdatePin(FEdGraphPinType& PinType) const;

	static void GetTablesDependentOnStruct(UScriptStruct* Struct, TArray<UDataTable*>& DataTables);

	friend FCSReload;

private:
	UFunction* FindMatchingMember(const FMemberReference& FunctionReference) const;
	bool UpdateMemberCall(UK2Node_CallFunction* Node) const;
	bool UpdateMemberCall(UK2Node_CSAsyncAction* Node) const;
	void UpdateInheritance(UBlueprint* Blueprint, bool& RefNeedsNodeReconstruction) const;
	void UpdateNodePinTypes(UEdGraphNode* Node, bool& RefNeedsNodeReconstruction) const;

	// Pending classes/interfaces to reinstance
	TMap<UClass*, UClass*> ClassesToReinstance;

	// Pending structs to reinstance
	TMap<UScriptStruct*, UScriptStruct*> StructsToReinstance;

	// Pending interfaces to reinstance
	TMap<UClass*, UClass*> InterfacesToReinstance;
	
};
