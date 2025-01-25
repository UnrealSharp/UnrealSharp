#include "CSDefaultComponentPropertyGenerator.h"
#include "CSObjectPropertyGenerator.h"
#include "Engine/InheritableComponentHandler.h"
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
	SimpleConstructionScript = &OuterClass->SimpleConstructionScript;
#endif
	
	AddDefaultComponentNode(OuterClass, SimpleConstructionScript, PropertyMetaData);

	UCSObjectPropertyGenerator* ObjectPropertyGenerator = GetMutableDefault<UCSObjectPropertyGenerator>();
	return ObjectPropertyGenerator->CreateProperty(Outer, PropertyMetaData);
}

void UCSDefaultComponentPropertyGenerator::AddDefaultComponentNode(UClass* Outer, TObjectPtr<USimpleConstructionScript>* SimpleConstructionScript, const FCSPropertyMetaData& PropertyMetaData)
{
	USimpleConstructionScript* CurrentSCS = SimpleConstructionScript->Get();
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
	USCS_Node* NewNode = NewObject<USCS_Node>(SimpleConstructionScript, MakeUniqueObjectName(SimpleConstructionScript, USCS_Node::StaticClass()));
	NewNode->SetFlags(RF_Transient);
	NewNode->SetVariableName(NewComponentVariableName, false);
	NewNode->VariableGuid = ConstructGUIDFromName(NewComponentVariableName);
	
	UpdateTemplateComponent(NewNode, GeneratedClass, NewComponentClass, NewComponentVariableName);
	
	SimpleConstructionScript->AddNode(NewNode);
	return NewNode;
}

void UCSDefaultComponentPropertyGenerator::UpdateTemplateComponent(USCS_Node* Node, UObject* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName)
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

void UCSDefaultComponentPropertyGenerator::UpdateChildren(UClass* Outer, USCS_Node* Node)
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
