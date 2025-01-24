#include "CSDefaultComponentPropertyGenerator.h"
#include "CSObjectPropertyGenerator.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

TSharedPtr<FCSUnrealType> UCSDefaultComponentPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDefaultComponentMetaData>();
}

FProperty* UCSDefaultComponentPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	UBlueprintGeneratedClass* OuterClass = static_cast<UBlueprintGeneratedClass*>(Outer);
	
	TObjectPtr<USimpleConstructionScript>* SimpleConstructionScript;
#if WITH_EDITOR
	UBlueprint* Blueprint = static_cast<UBlueprint*>(OuterClass->ClassGeneratedBy.Get());
	OuterClass = static_cast<UBlueprintGeneratedClass*>(Blueprint->GeneratedClass.Get());
	SimpleConstructionScript = &Blueprint->SimpleConstructionScript;
#else
	SimpleConstructionScript = &OuterClass->SimpleConstructionScript;;
#endif
	
	AddDefaultComponentNode(OuterClass, SimpleConstructionScript, PropertyMetaData);

	UCSObjectPropertyGenerator* ObjectPropertyGenerator = GetMutableDefault<UCSObjectPropertyGenerator>();
	return ObjectPropertyGenerator->CreateProperty(Outer, PropertyMetaData);
}

void UCSDefaultComponentPropertyGenerator::AddDefaultComponentNode(UObject* Outer, TObjectPtr<USimpleConstructionScript>* SimpleConstructionScript, const FCSPropertyMetaData& PropertyMetaData)
{
	USimpleConstructionScript* CurrentSCS = SimpleConstructionScript->Get();
	if (!IsValid(CurrentSCS))
	{
		CurrentSCS = NewObject<USimpleConstructionScript>(Outer, NAME_None, RF_Transactional);
		*SimpleConstructionScript = CurrentSCS;
	}
	
	TSharedPtr<FCSDefaultComponentMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSDefaultComponentMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);

	USCS_Node* Node = CurrentSCS->FindSCSNode(PropertyMetaData.Name);
	
	if (!Node)
	{
		Node = CreateNode(CurrentSCS, Outer, Class, PropertyMetaData.Name);
	}
	
	FName AttachToComponentName = ObjectMetaData->AttachmentComponent;
	bool HasValidAttachment = AttachToComponentName != "None";
	
	Node->AttachToName = HasValidAttachment ? ObjectMetaData->AttachmentSocket : NAME_None;
	Node->ComponentClass = Class;

	if (HasValidAttachment)
	{
		USCS_Node* ParentNode = CurrentSCS->FindSCSNode(AttachToComponentName);

		if (!ParentNode)
		{
			ParentNode = CurrentSCS->GetRootNodes()[0];
		}
		
		if (ParentNode->ChildNodes.Contains(Node))
		{
			return;
		}
		
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

USCS_Node* UCSDefaultComponentPropertyGenerator::CreateNode(USimpleConstructionScript* SimpleConstructionScript, UObject* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName)
{
	UPackage* TransientPackage = GetTransientPackage();
	UActorComponent* NewComponentTemplate = NewObject<UActorComponent>(TransientPackage, NewComponentClass, NAME_None, RF_ArchetypeObject | RF_Transactional | RF_Public);
	NewComponentTemplate->Modify();

	FString Name = NewComponentVariableName.ToString() + TEXT("_GEN_VARIABLE");
	UObject* Collision = FindObject<UObject>(GeneratedClass, *Name);
	
	while(Collision)
	{
		Collision->Rename(nullptr, GetTransientPackage(), REN_DoNotDirty | REN_DontCreateRedirectors);
		Collision = FindObject<UObject>(GeneratedClass, *Name);
	}

	NewComponentTemplate->Rename(*Name, GeneratedClass, REN_DoNotDirty | REN_DontCreateRedirectors);

	USCS_Node* NewNode = NewObject<USCS_Node>(SimpleConstructionScript, MakeUniqueObjectName(SimpleConstructionScript, USCS_Node::StaticClass()));
	NewNode->SetFlags(RF_Transactional);
	NewNode->ComponentClass = NewComponentTemplate->GetClass();
	NewNode->ComponentTemplate = NewComponentTemplate;
	NewNode->SetVariableName(NewComponentVariableName, false);
	NewNode->VariableGuid = ConstructGUIDFromName(NewComponentVariableName);
	
	SimpleConstructionScript->AddNode(NewNode);
	
	return NewNode;
}
