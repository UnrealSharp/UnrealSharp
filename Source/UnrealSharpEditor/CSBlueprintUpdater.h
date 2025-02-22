#pragma once

class UCSClass;
class UK2Node_CSAsyncAction;
class UK2Node_CallFunction;
class UClass;

class FCSBlueprintUpdater final
{
public:
	
	static FCSBlueprintUpdater& Get();
	
	void Initialize();
	void UpdateBlueprints();

private:

	void OnNewStruct(UScriptStruct* NewStruct);

	bool TryUpdatePin(const FEdGraphPinType& PinType) const;
	bool UpdateNodePinTypes(UEdGraphNode* Node) const;
	
	TSet<UScriptStruct*> UpdatedStructs;
};
