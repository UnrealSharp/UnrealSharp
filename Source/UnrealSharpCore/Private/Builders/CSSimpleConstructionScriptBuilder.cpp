#include "Builders/CSSimpleConstructionScriptBuilder.h"
#include "Engine/InheritableComponentHandler.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "MetaData/CSDefaultComponentMetaData.h"
#include "MetaData/CSPropertyMetaData.h"
#include "UnrealSharpUtils.h"
#include "Utilities/CSClassUtilities.h"

FCSRootNodeInfo::FCSRootNodeInfo(const USCS_Node* Node, const USimpleConstructionScript* CurrentSCS)
{
	Name = Node->GetVariableName();
	IsNative = Node->bIsParentComponentNative;
	IsInOtherSCS = Node->GetOuter() != CurrentSCS;
	OwningClass = Node->GetSCS()->GetOwnerClass();
}

FCSRootNodeInfo::FCSRootNodeInfo(const FObjectProperty* NativeProperty)
{
	Name = NativeProperty->GetFName();
	OwningClass = NativeProperty->GetOwnerClass();
	IsNative = true;
	IsInOtherSCS = true;

	ensure(OwningClass->HasAllClassFlags(CLASS_Native));
}

void FCSSimpleConstructionScriptBuilder::BuildSimpleConstructionScript(UClass* Outer, TObjectPtr<USimpleConstructionScript>* SimpleConstructionScript, const TArray<FCSPropertyMetaData>& PropertyMetaDatas)
{
	USimpleConstructionScript* CurrentSCS = SimpleConstructionScript->Get();
	UBlueprintGeneratedClass* GeneratedClass = Cast<UBlueprintGeneratedClass>(Outer);
	
	TArray<FCSAttachmentNode> AttachmentNodes;
	TArray<FCSNodeInfo> AllNodes;
	
	FCSRootNodeInfo RootComponentNode;
	
	for (const FCSPropertyMetaData& PropertyMetaData : PropertyMetaDatas)
	{
		TSharedPtr<FCSDefaultComponentMetaData> ObjectMetaData = PropertyMetaData.SafeCastTypeMetaData<FCSDefaultComponentMetaData>(ECSPropertyType::DefaultComponent);

		if (!ObjectMetaData.IsValid())
		{
			continue;
		}
		
		if (!IsValid(CurrentSCS))
		{
			CurrentSCS = NewObject<USimpleConstructionScript>(Outer);
			*SimpleConstructionScript = CurrentSCS;
		}
		
		UClass* Class = ObjectMetaData->InnerType.GetAsClass();
		USCS_Node* Node = CurrentSCS->FindSCSNode(PropertyMetaData.GetName());
	
		if (!IsValid(Node))
		{
			Node = CreateNode(CurrentSCS, Outer, Class, PropertyMetaData.GetName());

			if (IsRootNode(ObjectMetaData, Node))
			{
				CurrentSCS->AddNode(Node);
			}
		}
		else if (Class != Node->ComponentClass)
		{
			UpdateChildren(Outer, Node);
			UpdateTemplateComponent(Node, Outer, Class, PropertyMetaData.GetName());
		}

		AllNodes.Add(FCSNodeInfo(Node, ObjectMetaData));

		bool bWasRootNode = Node->IsRootNode();
		bool bIsRootNodeNow = IsRootNode(ObjectMetaData, Node);

		if (bWasRootNode && !bIsRootNodeNow)
		{
			// Node used to be root but no longer is, remove it from root
			CurrentSCS->RemoveNode(Node, false);
		}
		else if (bWasRootNode && bIsRootNodeNow)
		{
			if (!RootComponentNode.IsValid() && Node->ComponentClass->IsChildOf<USceneComponent>())
			{
				RootComponentNode = FCSRootNodeInfo(Node, CurrentSCS);
			}
			
			continue;
		}

		FCSAttachmentNode AttachmentNode;
		AttachmentNode.Node = Node;
		AttachmentNode.AttachToComponentName = ObjectMetaData->AttachmentComponent;
		AttachmentNodes.Add(AttachmentNode);
		Node->AttachToName = ObjectMetaData->AttachmentSocket;
	}

	if (!IsValid(CurrentSCS))
	{
		return;
	}

	if (!RootComponentNode.IsValid())
	{
		// User has not specified a root component, try to find or promote one
		TryFindOrPromoteRootComponent(CurrentSCS, RootComponentNode, GeneratedClass, AllNodes);
	}

	USCS_Node* DefaultSceneRootComponent = FindObject<USCS_Node>(CurrentSCS, *DefaultSceneRoot_UnrealSharp);
	if (IsValid(DefaultSceneRootComponent) && RootComponentNode.Name != DefaultSceneRootComponent->GetVariableName())
	{
		// New user-defined root component has been found, remove the default one
		CurrentSCS->RemoveNode(DefaultSceneRootComponent, false);
		DefaultSceneRootComponent->MarkAsGarbage();
	}

	for (const FCSAttachmentNode& AttachmentNode : AttachmentNodes)
	{
		if (AttachmentNode.Node->IsRootNode())
		{
			// Node was promoted to root in TryFindOrPromoteRootComponent, skip it
			continue;
		}
		
		if (const FObjectProperty* ObjectProperty = FindFProperty<FObjectProperty>(Outer, AttachmentNode.AttachToComponentName))
		{
			UClass* ParentClass = ObjectProperty->GetOwnerClass();
			
			if (FCSClassUtilities::IsNativeClass(ParentClass))
			{
				UObject* DefaultObject = ParentClass->GetDefaultObject();
				UObject* Component = ObjectProperty->GetObjectPropertyValue_InContainer(DefaultObject);

				if (ObjectProperty && IsValid(Component) && Component->IsA<USceneComponent>())
				{
					RootComponentNode = FCSRootNodeInfo(ObjectProperty);
				}
			}
			else
			{
				USimpleConstructionScript* ParentSimpleConstructionComponent;
				USCS_Node* ParentNode;
				if (TryFindParentNodeAndComponent(AttachmentNode.AttachToComponentName, Outer, ParentNode, ParentSimpleConstructionComponent))
				{
					RootComponentNode = FCSRootNodeInfo(ParentNode, ParentSimpleConstructionComponent);
				}
			}
		}
		
		USCS_Node* NodeToAttach = AttachmentNode.Node;
		
		if (RootComponentNode.IsNative || RootComponentNode.IsInOtherSCS)
		{
			NodeToAttach->bIsParentComponentNative = RootComponentNode.IsNative;
			NodeToAttach->ParentComponentOrVariableName = RootComponentNode.Name;
			NodeToAttach->ParentComponentOwnerClassName = RootComponentNode.OwningClass->GetFName();

			// Is considered a root node if parent is native or in another SCS
			CurrentSCS->AddNode(NodeToAttach);
		}
		else
		{
			USCS_Node* RootNode = CurrentSCS->FindSCSNode(RootComponentNode.Name);
				
			if (!RootNode->ChildNodes.Contains(NodeToAttach))
			{
				bool bAddToAllNodes = !CurrentSCS->GetAllNodes().Contains(NodeToAttach);
				RootNode->AddChildNode(NodeToAttach, bAddToAllNodes);
			}
		}

#if WITH_EDITOR
		const TArray<USCS_Node*>& AllRegisteredNodes = CurrentSCS->GetAllNodes();
		for (USCS_Node* NodeIterator : AllRegisteredNodes)
		{
			FName ParentComponentName = NodeIterator->GetVariableName();

			if (NodeIterator == NodeToAttach)
			{
				continue;
			}

			if (AttachmentNode.AttachToComponentName == NAME_None || ParentComponentName != AttachmentNode.AttachToComponentName)
			{
				continue;
			}

			if (NodeIterator->ChildNodes.Contains(NodeToAttach))
			{
				// The node is already correctly attached
				break;
			}
			
			// The attachment has changed, remove the node from the old parent
			NodeIterator->RemoveChildNode(NodeToAttach, false);
			break;
		}
#endif
	}
}

USCS_Node* FCSSimpleConstructionScriptBuilder::CreateNode(USimpleConstructionScript* SimpleConstructionScript, UStruct* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName, FString* OptionalName)
{
	FName NodeName = OptionalName ? FName(*OptionalName) : MakeUniqueObjectName(SimpleConstructionScript, USCS_Node::StaticClass());
	USCS_Node* NewNode = NewObject<USCS_Node>(SimpleConstructionScript, NodeName);
	
	NewNode->SetFlags(RF_Transient);
	NewNode->SetVariableName(NewComponentVariableName, false);
	NewNode->VariableGuid = FCSUnrealSharpUtils::ConstructGUIDFromName(NewComponentVariableName);
	
	UpdateTemplateComponent(NewNode, GeneratedClass, NewComponentClass, NewComponentVariableName);

	return NewNode;
}

void FCSSimpleConstructionScriptBuilder::UpdateTemplateComponent(USCS_Node* Node, UStruct* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName)
{
	UPackage* TransientPackage = GetTransientPackage();
	const FName TemplateName(*FString::Printf(TEXT("%s_GEN_VARIABLE"), *NewComponentVariableName.ToString()));

	UObject* OldTemplateObject = IsValid(Node->ComponentTemplate) ? FindObjectFast<UObject>(GeneratedClass, TemplateName) : nullptr;

	if (IsValid(OldTemplateObject))
	{
		OldTemplateObject->Rename(nullptr, TransientPackage, REN_DoNotDirty | REN_DontCreateRedirectors);
	}

	constexpr EObjectFlags NewObjectFlags = RF_ArchetypeObject | RF_Public;
	UActorComponent* NewComponentTemplate = NewObject<UActorComponent>(GeneratedClass, NewComponentClass, NAME_None, NewObjectFlags);

#if WITH_EDITOR
	if (ICSManagedTypeInterface* ManagedType = FCSClassUtilities::GetManagedType(NewComponentClass))
	{
		ManagedType->GetManagedReferencesCollection().AddReference(GeneratedClass);
	}
#endif

	Node->ComponentClass = NewComponentClass;
	Node->ComponentTemplate = NewComponentTemplate;

	if (!IsValid(OldTemplateObject))
	{
		return;
	}

	const UClass* OldClass = OldTemplateObject->GetClass();
	const UClass* NewClass = NewComponentTemplate->GetClass();

	constexpr EPropertyFlags SkipFlags = CPF_Transient | CPF_DuplicateTransient | CPF_TextExportTransient;
	for (TFieldIterator<FProperty> It(NewClass, EFieldIteratorFlags::IncludeSuper); It; ++It)
	{
		const FProperty* Property = *It;
			
		if (Property->PropertyFlags & SkipFlags)
		{
			continue;
		}

		const FProperty* OldProp = OldClass->FindPropertyByName(Property->GetFName());
		if (!OldProp || !OldProp->SameType(Property) || OldProp->HasAnyPropertyFlags(SkipFlags))
		{
			continue;
		}

		void* Destination = Property->ContainerPtrToValuePtr<void>(NewComponentTemplate);
		void* Source = OldProp->ContainerPtrToValuePtr<void>(OldTemplateObject);
		Property->CopyCompleteValue(Destination, Source);
	}

	OldTemplateObject->MarkAsGarbage();
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
		if (FCSUnrealSharpUtils::IsStandalonePIE())
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

		if (!IsValid(CurrentSCS))
		{
			continue;
		}
		
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

bool FCSSimpleConstructionScriptBuilder::IsRootNode(const TSharedPtr<FCSDefaultComponentMetaData>& ObjectMetaData, const USCS_Node* Node)
{
	if (Node->IsRootNode())
	{
		return true;
	}
	
	if (ObjectMetaData->IsRootComponent)
	{
		// Root components and components with no valid attachment are just root nodes.
		return true;
	}

	if (!Node->ComponentClass->IsChildOf(USceneComponent::StaticClass()))
	{
		// Any component without a transform is a root node.
		return true;
	}

	return false;
}

void FCSSimpleConstructionScriptBuilder::ForEachSimpleConstructionScript(USimpleConstructionScript* SimpleConstructionScript, TFunctionRef<bool(USimpleConstructionScript*)> Callback)
{
	for (UClass* CurrentClass = SimpleConstructionScript->GetOwnerClass(); CurrentClass; CurrentClass = CurrentClass->GetSuperClass())
	{
		UBlueprintGeneratedClass* CurrentGeneratedClass = Cast<UBlueprintGeneratedClass>(CurrentClass);
		if (!IsValid(CurrentGeneratedClass))
		{
			return;
		}

		USimpleConstructionScript* CurrentSCS = CurrentGeneratedClass->SimpleConstructionScript;
		if (!IsValid(CurrentSCS))
		{
			continue;
		}

		if (!Callback(CurrentSCS))
		{
			return;
		}
	}
}

USCS_Node* FCSSimpleConstructionScriptBuilder::FindRootComponentNode(USimpleConstructionScript* SimpleConstructionScript)
{
	for (USCS_Node* Node : SimpleConstructionScript->GetRootNodes())
	{
		if (Node->ComponentClass->IsChildOf<USceneComponent>())
		{
			return Node;
		}
	}

	return nullptr;
}

void FCSSimpleConstructionScriptBuilder::TryFindOrPromoteRootComponent(USimpleConstructionScript* SimpleConstructionScript, FCSRootNodeInfo& RootComponentNode, UBlueprintGeneratedClass* Outer, const TArray<FCSNodeInfo>& AllNodes)
{
	ForEachSimpleConstructionScript(SimpleConstructionScript, [&](USimpleConstructionScript* ParentSCS)
	{
		// See if the parent SCS has a root component we can use
		USCS_Node* ParentRootNode = FindRootComponentNode(ParentSCS);
				
		if (IsValid(ParentRootNode) && ParentRootNode->GetName() == DefaultSceneRoot_UnrealSharp)
		{
			RootComponentNode = FCSRootNodeInfo(ParentRootNode, SimpleConstructionScript);
			return false;
		}
		return true;
	});

	if (!RootComponentNode.IsValid())
	{
		// See if the actor's default root component can be used
		UClass* FirstNativeClass = FCSClassUtilities::GetFirstNativeClass(Outer);
		AActor* DefaultActor = Cast<AActor>(FirstNativeClass->GetDefaultObject());
			
		if (IsValid(DefaultActor) && IsValid(DefaultActor->GetRootComponent()))
		{
			USceneComponent* RootComponent = DefaultActor->GetRootComponent();

			for (TFieldIterator<FObjectProperty> It (FirstNativeClass); It; ++It)
			{
				FObjectProperty* Property = *It;
				UObject* Value = Property->GetObjectPropertyValue_InContainer(DefaultActor);
					
				if (Value != RootComponent)
				{
					continue;
				}

				RootComponentNode = FCSRootNodeInfo(Property);
				return;
			}
		}
	}

	if (!RootComponentNode.IsValid())
	{
		// See if we can promote an existing component to be the root
		for (const FCSNodeInfo& NodeInfo : AllNodes)
		{
			bool IsSceneComponent = NodeInfo.Node->ComponentClass->IsChildOf<USceneComponent>();
			bool HasSocket = NodeInfo.ObjectMetaData->AttachmentSocket != NAME_None;
			bool IsAttached = NodeInfo.ObjectMetaData->AttachmentComponent != NAME_None;
				
			if (!IsSceneComponent || HasSocket || IsAttached)
			{
				continue;
			}

			RootComponentNode = FCSRootNodeInfo(NodeInfo.Node, SimpleConstructionScript);
			SimpleConstructionScript->AddNode(NodeInfo.Node);
			return;
		}
	}

	if (!RootComponentNode.IsValid())
	{
		// Last resort, make a default scene root
		USCS_Node* Node = CreateNode(SimpleConstructionScript, Outer, USceneComponent::StaticClass(), "DefaultSceneRoot", &DefaultSceneRoot_UnrealSharp);
		SimpleConstructionScript->AddNode(Node);
		RootComponentNode = FCSRootNodeInfo(Node, SimpleConstructionScript);
	}
}
