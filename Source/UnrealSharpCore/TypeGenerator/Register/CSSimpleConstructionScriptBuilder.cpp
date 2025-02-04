#include "CSSimpleConstructionScriptBuilder.h"
#include "Engine/InheritableComponentHandler.h"

#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/Factories/PropertyGenerators/CSPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

void FCSSimpleConstructionScriptBuilder::BuildSimpleConstructionScript(UClass* Outer, TObjectPtr<USimpleConstructionScript>* SimpleConstructionScript, const TArray<FCSPropertyMetaData>& PropertyMetaDatas)
{
	struct FCSAttachmentNode
	{
		USCS_Node* Node;
		FName AttachToComponentName;
	};
	
	USimpleConstructionScript* CurrentSCS = SimpleConstructionScript->Get();
	TArray<FCSAttachmentNode> AttachmentNodes;
	
	for (const FCSPropertyMetaData& PropertyMetaData : PropertyMetaDatas)
	{
		if (PropertyMetaData.Type->PropertyType != ECSPropertyType::DefaultComponent)
		{
			continue;
		}
		
		if (!IsValid(CurrentSCS))
		{
			CurrentSCS = NewObject<USimpleConstructionScript>(Outer, NAME_None, RF_Transient);
			*SimpleConstructionScript = CurrentSCS;
		}
	
		TSharedPtr<FCSDefaultComponentMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSDefaultComponentMetaData>();
		UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);

		USCS_Node* Node = CurrentSCS->FindSCSNode(PropertyMetaData.Name);
	
		if (!Node)
		{
			Node = CreateNode(CurrentSCS, Outer, Class, PropertyMetaData.Name);
		}
		else if (Class != Node->ComponentClass)
		{
			UpdateChildren(Outer, Node);
			UpdateTemplateComponent(Node, Outer, Class, PropertyMetaData.Name);
		}
	
		FName AttachToComponentName = ObjectMetaData->AttachmentComponent;
		bool HasValidAttachment = AttachToComponentName != "None";
	
		Node->AttachToName = HasValidAttachment ? ObjectMetaData->AttachmentSocket : NAME_None;

		if (HasValidAttachment)
		{
			FCSAttachmentNode AttachmentNode;
			AttachmentNode.Node = Node;
			AttachmentNode.AttachToComponentName = AttachToComponentName;
			AttachmentNodes.Add(AttachmentNode);
		}
	}

	for (const FCSAttachmentNode& AttachmentNode : AttachmentNodes)
	{
		FName AttachToComponentName = AttachmentNode.AttachToComponentName;
		USCS_Node* Node = AttachmentNode.Node;
		USCS_Node* ParentNode = CurrentSCS->FindSCSNode(AttachToComponentName);

		if (!ParentNode)
		{
			ParentNode = CurrentSCS->GetRootNodes()[0];
		}
		
		if (ParentNode->ChildNodes.Contains(Node))
		{
			return;
		}

		ParentNode->AddChildNode(Node);
		
		Node->bIsParentComponentNative = false;
		Node->ParentComponentOrVariableName = AttachToComponentName;
		Node->ParentComponentOwnerClassName = SimpleConstructionScript->GetFName();
		
		for (USCS_Node* NodeItr : CurrentSCS->GetAllNodes())
		{
			if (NodeItr != Node && NodeItr->ChildNodes.Contains(Node) && NodeItr->GetVariableName() != AttachToComponentName)
			{
				// The attachment has changed, remove the node from the old parent
				NodeItr->RemoveChildNode(Node, false);
				break;
			}
		}
	}
}

USCS_Node* FCSSimpleConstructionScriptBuilder::CreateNode(USimpleConstructionScript* SimpleConstructionScript, UObject* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName)
{
	USCS_Node* NewNode = NewObject<USCS_Node>(SimpleConstructionScript, MakeUniqueObjectName(SimpleConstructionScript, USCS_Node::StaticClass()));
	NewNode->SetFlags(RF_Transient);
	NewNode->SetVariableName(NewComponentVariableName, false);
	NewNode->VariableGuid = UCSPropertyGenerator::ConstructGUIDFromName(NewComponentVariableName);
	
	UpdateTemplateComponent(NewNode, GeneratedClass, NewComponentClass, NewComponentVariableName);
	
	SimpleConstructionScript->AddNode(NewNode);
	return NewNode;
}

void FCSSimpleConstructionScriptBuilder::UpdateTemplateComponent(USCS_Node* Node, UObject* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName)
{
	UPackage* TransientPackage = GetTransientPackage();
	UActorComponent* NewComponentTemplate = NewObject<UActorComponent>(TransientPackage, NewComponentClass, NAME_None, RF_ArchetypeObject | RF_Public);

	FString Name = NewComponentVariableName.ToString() + TEXT("_GEN_VARIABLE");
	UObject* Collision = FindObject<UObject>(GeneratedClass, *Name);
	
	while (Collision)
	{
		Collision->Rename(nullptr, TransientPackage, REN_DoNotDirty | REN_DontCreateRedirectors);
		Collision = FindObject<UObject>(GeneratedClass, *Name);
	}

	NewComponentTemplate->Rename(*Name, GeneratedClass, REN_DoNotDirty | REN_DontCreateRedirectors);
	
	Node->ComponentClass = NewComponentTemplate->GetClass();
	Node->ComponentTemplate = NewComponentTemplate;
}

void FCSSimpleConstructionScriptBuilder::UpdateChildren(UClass* Outer, USCS_Node* Node)
{
	// Unreal's component system doesn't support changing the component class of a node, unless you remove and then re-add the node
	// This is a workaround to not make the system consider our new template as garbage as it's not yet used in the UInheritableComponentHandler
#if WITH_EDITOR
	FComponentKey ComponentKey(Node);
	TArray<UClass*> ChildClasses;
	GetDerivedClasses(Outer, ChildClasses);

	for (const UClass* ChildClass : ChildClasses)
	{
		const UBlueprint* Blueprint = Cast<UBlueprint>(ChildClass->ClassGeneratedBy);

		if (!IsValid(Blueprint))
		{
			continue;
		}

		UActorComponent* Template = Blueprint->InheritableComponentHandler->GetOverridenComponentTemplate(ComponentKey);
			
		if (!IsValid(Template))
		{
			continue;
		}
		
		Template->Rename(nullptr, GetTransientPackage(), REN_DoNotDirty | REN_DontCreateRedirectors);
		Template->ClearFlags(RF_Standalone);
		Template->RemoveFromRoot();
		
		Blueprint->InheritableComponentHandler->RemoveOverridenComponentTemplate(ComponentKey);
	}
#endif
}
