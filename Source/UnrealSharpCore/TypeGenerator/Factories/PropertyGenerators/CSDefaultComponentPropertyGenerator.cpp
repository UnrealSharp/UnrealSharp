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
	if (!IsValid(Node) || Node->ComponentClass != Class)
	{
		if (IsValid(Node))
		{
			SimpleConstructionScript->RemoveNode(Node);
		}
		
		Node = SimpleConstructionScript->CreateNode(Class, PropertyMetaData.Name);
	}

	Node->AttachToName = ObjectMetaData->AttachmentComponent;
	Node->SetVariableName(PropertyMetaData.Name, false);
	Node->ComponentClass = Class;
	Node->VariableGuid = ConstructGUIDFromName(PropertyMetaData.Name);

	if (USCS_Node* ParentNode = SimpleConstructionScript->FindSCSNode(ObjectMetaData->AttachmentComponent))
	{
		ParentNode->AddChildNode(Node);
	}
	
	SimpleConstructionScript->AddNode(Node);
}

TSharedPtr<FCSUnrealType> UCSDefaultComponentPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDefaultComponentMetaData>();
}
