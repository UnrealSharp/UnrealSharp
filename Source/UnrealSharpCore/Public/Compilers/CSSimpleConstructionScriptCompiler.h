#pragma once

struct FCSDefaultComponentType;
struct FCSPropertyReflectionData;
class USCS_Node;

struct FCSAttachmentNode
{
	USCS_Node* Node;
	FName AttachToComponentName;
};

struct FCSRootNodeInfo
{
	FName Name;
	bool IsNative = false;
	bool IsInOtherSCS = false;
	UClass* OwningClass = nullptr;

	FCSRootNodeInfo(const USCS_Node* Node, const USimpleConstructionScript* CurrentSCS);
	FCSRootNodeInfo(const FObjectProperty* NativeProperty, USceneComponent* Component);
	FCSRootNodeInfo() {}

	bool IsValid() const
	{
		return Name != NAME_None;
	}
};

struct FCSNodeInfo
{
	USCS_Node* Node;
	TSharedPtr<FCSDefaultComponentType> DefaultComponentData;

	FCSNodeInfo(USCS_Node* InNode, TSharedPtr<FCSDefaultComponentType> InDefaultComponentData)
		: Node(InNode)
		, DefaultComponentData(InDefaultComponentData)
	{
	}
};


class FCSSimpleConstructionScriptCompiler
{
public:
	UNREALSHARPCORE_API static void CompileSimpleConstructionScript(UClass* Outer, TObjectPtr<USimpleConstructionScript>* SimpleConstructionScript, const TArray<FCSPropertyReflectionData>& PropertiesReflectionData);
private:
	static USCS_Node* CreateNode(USimpleConstructionScript* SimpleConstructionScript, UStruct* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName, FString* OptionalName = nullptr);
	static void UpdateTemplateComponent(USCS_Node* Node, UStruct* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName);
	static void UpdateChildren(UClass* Outer, USCS_Node* Node);
	static bool TryFindParentNodeAndComponent(FName ParentComponentName, UClass* ClassToSearch, USCS_Node*& OutNode, USimpleConstructionScript*& OutSCS);
	static bool IsRootNode(const TSharedPtr<FCSDefaultComponentType>& DefaultComponentData, const USCS_Node* Node);
	
	static void ForEachSimpleConstructionScript(USimpleConstructionScript* SimpleConstructionScript, TFunctionRef<bool(USimpleConstructionScript*)> Callback);
	static USCS_Node* FindRootComponentNode(USimpleConstructionScript* SimpleConstructionScript);

	static void TryFindOrPromoteRootComponent(USimpleConstructionScript* SimpleConstructionScript, FCSRootNodeInfo& RootComponentNode, UBlueprintGeneratedClass* Outer, const TArray<FCSNodeInfo>& AllNodes);
	
	static inline FString DefaultSceneRoot_UnrealSharp = TEXT("DefaultSceneRoot_UnrealSharp");
};
