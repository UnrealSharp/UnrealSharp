#pragma once

struct FCSPropertyMetaData;

class FCSSimpleConstructionScriptBuilder
{
public:
	UNREALSHARPCORE_API static void BuildSimpleConstructionScript(UClass* Outer, TObjectPtr<USimpleConstructionScript>* SimpleConstructionScript, const TArray<FCSPropertyMetaData>& PropertyMetaDatas);
private:
	static USCS_Node* CreateNode(USimpleConstructionScript* SimpleConstructionScript, UObject* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName);
	static void UpdateTemplateComponent(USCS_Node* Node, UObject* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName);
	static void UpdateChildren(UClass* Outer, USCS_Node* Node);
	static bool TryFindParentNodeAndComponent(FName ParentComponentName, UClass* ClassToSearch, USCS_Node*& OutNode, USimpleConstructionScript*& OutSCS);
};
