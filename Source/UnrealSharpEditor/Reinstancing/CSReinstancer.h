#pragma once

class UCSClass;
class UK2Node_CSAsyncAction;
class UK2Node_CallFunction;
class UClass;

class FCSReinstancer final
{
public:
	
	static FCSReinstancer& Get();
	
	void Initialize();
	
	void FinishHotReload();
	void FixDataTables();
	void UpdateBlueprints();

private:

	bool TryUpdatePin(FEdGraphPinType& PinType) const;

	void GetTablesDependentOnStruct(UScriptStruct* Struct, TArray<UDataTable*>& DataTables);

	void AddPendingClass(UClass* NewClass);
	void AddPendingStruct(UScriptStruct* NewStruct);
	void AddPendingInterface(UClass* NewInterface);
	
	UFunction* FindMatchingMember(const FMemberReference& FunctionReference) const;
	bool UpdateMemberCall(UK2Node_CallFunction* Node) const;
	bool UpdateMemberCall(UK2Node_CSAsyncAction* Node) const;
	void UpdateInheritance(UBlueprint* Blueprint, bool& RefNeedsNodeReconstruction) const;
	void UpdateNodePinTypes(UEdGraphNode* Node, bool& RefNeedsNodeReconstruction) const;
	
	TSet<UClass*> ClassesToReinstance;
	TSet<UScriptStruct*> StructsToReinstance;
	TSet<UClass*> InterfacesToReinstance;
};
