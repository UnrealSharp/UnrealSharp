#include "CSDefaultComponentPropertyGenerator.h"

#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

void UCSDefaultComponentPropertyGenerator::CreatePropertyEditor(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData)
{
	if (!IsValid(Blueprint->SimpleConstructionScript))
	{
		Blueprint->SimpleConstructionScript = NewObject<USimpleConstructionScript>(Blueprint->GeneratedClass);
	}
	
	USimpleConstructionScript* SimpleConstructionScript = Blueprint->SimpleConstructionScript;

	TSharedPtr<FCSDefaultComponentMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSDefaultComponentMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);

	USCS_Node* Node = SimpleConstructionScript->FindSCSNode(PropertyMetaData.Name);
	
	if (!Node)
	{
		Node = SimpleConstructionScript->CreateNode(Class, PropertyMetaData.Name);
		Node->SetVariableName(PropertyMetaData.Name, false);
		SimpleConstructionScript->AddNode(Node);
	}
	
	FName AttachToComponentName = ObjectMetaData->AttachmentComponent;
	bool HasValidAttachment = AttachToComponentName != "None";
	
	Node->AttachToName = HasValidAttachment ? ObjectMetaData->AttachmentSocket : NAME_None;
	Node->ComponentClass = Class;

	if (HasValidAttachment)
	{
		USCS_Node* ParentNode = SimpleConstructionScript->FindSCSNode(AttachToComponentName);
		if (ParentNode->ChildNodes.Contains(Node))
		{
			return;
		}
		
		Node->bIsParentComponentNative = false;
		Node->ParentComponentOrVariableName = AttachToComponentName;
		Node->ParentComponentOwnerClassName = Blueprint->GeneratedClass->GetFName();
		
		for (USCS_Node* NodeItr : SimpleConstructionScript->GetAllNodes())
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

TSharedPtr<FCSUnrealType> UCSDefaultComponentPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDefaultComponentMetaData>();
}
