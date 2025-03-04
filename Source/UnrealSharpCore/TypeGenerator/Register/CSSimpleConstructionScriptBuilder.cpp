#include "CSSimpleConstructionScriptBuilder.h"

#include "CSGeneratedClassBuilder.h"
#include "UnrealSharpCore.h"
#include "Engine/InheritableComponentHandler.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/Factories/PropertyGenerators/CSPropertyGenerator.h"
#include "TypeGenerator/Register/MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

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
		UClass* Class = ObjectMetaData->InnerType.GetOwningClass();

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
		
		USCS_Node* ParentNode = nullptr;
		USimpleConstructionScript* ParentSimpleConstructionComponent = nullptr;
		if (TryFindParentNodeAndComponent(AttachToComponentName, Outer, ParentNode, ParentSimpleConstructionComponent))
		{
			if (ParentSimpleConstructionComponent == CurrentSCS)
			{
				if (!ParentNode->ChildNodes.Contains(Node))
				{
					ParentNode->AddChildNode(Node, false);
				}
			}
			else
			{
				Node->bIsParentComponentNative = false;
				Node->ParentComponentOrVariableName = AttachToComponentName;
				Node->ParentComponentOwnerClassName = ParentSimpleConstructionComponent->GetOwnerClass()->GetFName();
			}
		}
		else
		{
			// If we can't find a node, it's defined in a native parent class and don't have a node for it.
			UClass* NativeParent = FCSGeneratedClassBuilder::GetFirstNativeClass(Outer);
			if (FObjectProperty* Property = CastField<FObjectProperty>(NativeParent->FindPropertyByName(AttachToComponentName)))
			{
				UActorComponent* Component = Cast<UActorComponent>(Property->GetObjectPropertyValue_InContainer(NativeParent->GetDefaultObject()));

				if (!IsValid(Component))
				{
					UE_LOG(LogUnrealSharp, Error, TEXT("Parent component %s not found in class %s"), *AttachToComponentName.ToString(), *NativeParent->GetName());
					continue;
				}
				
				if (Component->GetFName() != AttachToComponentName)
				{
					Node->ParentComponentOwnerClassName = Component->GetClass()->GetFName();
					Node->ParentComponentOrVariableName = Component->GetFName();
				}
				else
				{
					Node->ParentComponentOrVariableName = AttachToComponentName;
					Node->ParentComponentOwnerClassName = Property->GetOwnerClass()->GetFName();
				}

				Node->bIsParentComponentNative = true;
			}
			else
			{
				UE_LOG(LogUnrealSharp, Error, TEXT("Parent component %s not found in class %s"), *AttachToComponentName.ToString(), *Outer->GetName());
			}
		}
		
		for (USCS_Node* NodeItr : CurrentSCS->GetAllNodes())
		{
			FName ParentComponentName = NodeItr->GetVariableName();
			
			if (NodeItr != Node && NodeItr->ChildNodes.Contains(Node) && ParentComponentName != AttachToComponentName)
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

bool FCSSimpleConstructionScriptBuilder::TryFindParentNodeAndComponent(FName ParentComponentName, UClass* ClassToSearch, USCS_Node*& OutNode, USimpleConstructionScript*& OutSCS)
{
	for (UClass* CurrentClass = ClassToSearch; CurrentClass; CurrentClass = CurrentClass->GetSuperClass())
	{
		UBlueprintGeneratedClass* CurrentGeneratedClass = Cast<UBlueprintGeneratedClass>(CurrentClass);
		if (!IsValid(CurrentGeneratedClass))
		{
			continue;
		}

		USimpleConstructionScript* CurrentSCS;
#if WITH_EDITOR
		if (FUnrealSharpUtils::IsStandalonePIE())
		{
			CurrentSCS = CurrentGeneratedClass->SimpleConstructionScript;
		}
		else
		{
			UBlueprint* Blueprint = Cast<UBlueprint>(CurrentGeneratedClass->ClassGeneratedBy);
			CurrentSCS = Blueprint->SimpleConstructionScript;
		}
#else
		CurrentSCS = CurrentGeneratedClass->SimpleConstructionScript;
#endif
		
		USCS_Node* FoundNode = CurrentSCS->FindSCSNode(ParentComponentName);
		if (!IsValid(FoundNode))
		{
			continue;
		}

		OutNode = FoundNode;
		OutSCS = CurrentSCS;
		return true;
	}

	return false;
}
