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
	static USCS_Node* GetParentNode(FName ParentComponentName, const UClass* ClassToSearch, const TArray<FCSNodeInfo>& AllNodes);
	static bool IsRootNode(const TSharedPtr<FCSDefaultComponentType>& DefaultComponentData, const USCS_Node* Node);
	
	static void ForEachSimpleConstructionScript(const USimpleConstructionScript* SimpleConstructionScript, const TFunctionRef<bool(USimpleConstructionScript*)>& Callback);
	static USCS_Node* FindRootComponentNode(const USimpleConstructionScript* SimpleConstructionScript);

	static void TryFindOrPromoteRootComponent(USimpleConstructionScript* SimpleConstructionScript, FCSRootNodeInfo& RootComponentNode, UBlueprintGeneratedClass* Outer, const TArray<FCSNodeInfo>& AllNodes);
	
	static void DetachNodeFromOldParent(USCS_Node* Node, USimpleConstructionScript* CurrentSCS, const FCSAttachmentNode& AttachmentNode);
	
	static USCS_Node* GetNodeByName(const TArray<FCSNodeInfo>& AllNodes, FName NodeName);
	
	static inline FString DefaultSceneRoot_UnrealSharp = TEXT("DefaultSceneRoot_UnrealSharp");
};
