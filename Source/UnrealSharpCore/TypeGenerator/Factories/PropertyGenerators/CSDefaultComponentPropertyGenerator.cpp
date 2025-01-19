#include "CSDefaultComponentPropertyGenerator.h"

#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

FGuid ConstructGUIDFromName(const FName& Name)
{
	const FString HashString = Name.ToString();
	const uint32 BufferLength = HashString.Len() * sizeof(HashString[0]);
	uint32 HashBuffer[5];
	FSHA1::HashBuffer(*HashString, BufferLength, reinterpret_cast<uint8*>(HashBuffer));
	return FGuid(HashBuffer[1], HashBuffer[2], HashBuffer[3], HashBuffer[4]);
}

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
