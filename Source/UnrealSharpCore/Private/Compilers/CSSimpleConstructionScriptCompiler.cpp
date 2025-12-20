#include "Compilers/CSSimpleConstructionScriptCompiler.h"
#include "Engine/InheritableComponentHandler.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "ReflectionData/CSDefaultComponentType.h"
#include "ReflectionData/CSPropertyReflectionData.h"
#include "UnrealSharpUtils.h"
#include "Utilities/CSClassUtilities.h"

FCSRootNodeInfo::FCSRootNodeInfo(const USCS_Node* Node, const USimpleConstructionScript* CurrentSCS)
{
	Name = Node->GetVariableName();
	IsNative = false;
	IsInOtherSCS = Node->GetOuter() != CurrentSCS;
	OwningClass = Node->GetSCS()->GetOwnerClass();
}

FCSRootNodeInfo::FCSRootNodeInfo(const FObjectProperty* NativeProperty, USceneComponent* Component)
{
	Name = Component->GetFName();
	OwningClass = NativeProperty->GetOwnerClass();
	IsNative = true;
	IsInOtherSCS = true;

	ensure(OwningClass->HasAllClassFlags(CLASS_Native));
}

void FCSSimpleConstructionScriptCompiler::CompileSimpleConstructionScript(UClass* Outer, TObjectPtr<USimpleConstructionScript>* SimpleConstructionScript, const TArray<FCSPropertyReflectionData>& PropertiesReflectionData)
{
	if (!Outer->IsChildOf(AActor::StaticClass()))
	{
		return;
	}
	
	UBlueprintGeneratedClass* GeneratedClass = static_cast<UBlueprintGeneratedClass*>(Outer);
	USimpleConstructionScript* CurrentSCS = SimpleConstructionScript->Get();
	
	if (!IsValid(CurrentSCS))
	{
		CurrentSCS = NewObject<USimpleConstructionScript>(Outer);
		*SimpleConstructionScript = CurrentSCS;
	}
	
	TArray<FCSAttachmentNode> AttachmentNodes;
	TArray<FCSNodeInfo> AllNodes;
	
	FCSRootNodeInfo ActorRootComponentInfo;
	
	for (const FCSPropertyReflectionData& PropertyReflectionData : PropertiesReflectionData)
	{
		TSharedPtr<FCSDefaultComponentType> DefaultComponentData = PropertyReflectionData.SafeCast<FCSDefaultComponentType>(ECSPropertyType::DefaultComponent);

		if (!DefaultComponentData.IsValid())
		{
			continue;
		}
		
		UClass* ComponentClass = DefaultComponentData->InnerType.GetAsClass();
		USCS_Node* ComponentNode = CurrentSCS->FindSCSNode(PropertyReflectionData.GetName());
	
		if (!IsValid(ComponentNode))
		{
			ComponentNode = CreateNode(CurrentSCS, Outer, ComponentClass, PropertyReflectionData.GetName());

			if (IsRootNode(DefaultComponentData, ComponentNode))
			{
				CurrentSCS->AddNode(ComponentNode);
			}
		}
		else if (ComponentClass != ComponentNode->ComponentClass)
		{
			UpdateChildren(Outer, ComponentNode);
			UpdateTemplateComponent(ComponentNode, Outer, ComponentClass, PropertyReflectionData.GetName());
		}

		AllNodes.Add(FCSNodeInfo(ComponentNode, DefaultComponentData));

		bool bWasRootNode = ComponentNode->IsRootNode();
		bool bIsRootNodeNow = IsRootNode(DefaultComponentData, ComponentNode);

		if (bWasRootNode && !bIsRootNodeNow)
		{
			// Node used to be root but no longer is, remove it from root
			CurrentSCS->RemoveNode(ComponentNode, false);
		}
		else if (bWasRootNode && bIsRootNodeNow)
		{
			if (!ActorRootComponentInfo.IsValid() && ComponentNode->ComponentClass->IsChildOf<USceneComponent>())
			{
				ActorRootComponentInfo = FCSRootNodeInfo(ComponentNode, CurrentSCS);
			}
			
			// Still a root node, nothing else to do. No attachment needed.
			continue;
		}

		FCSAttachmentNode AttachmentNode;
		AttachmentNode.Node = ComponentNode;
		AttachmentNode.AttachToComponentName = DefaultComponentData->AttachmentComponent;
		AttachmentNodes.Add(AttachmentNode);
		ComponentNode->AttachToName = DefaultComponentData->AttachmentSocket;
	}

	if (!ActorRootComponentInfo.IsValid())
	{
		// User has not specified a root component, try to find or promote one
		TryFindOrPromoteRootComponent(CurrentSCS, ActorRootComponentInfo, GeneratedClass, AllNodes);
	}

	USCS_Node* DefaultSceneRootComponent = FindObject<USCS_Node>(CurrentSCS, *DefaultSceneRoot_UnrealSharp);
	if (IsValid(DefaultSceneRootComponent))
	{
		if (ActorRootComponentInfo.Name != DefaultSceneRootComponent->GetVariableName())
		{
			CurrentSCS->RemoveNode(DefaultSceneRootComponent, false);
			DefaultSceneRootComponent->MarkAsGarbage();
		}
		else
		{
			AllNodes.Add(FCSNodeInfo(DefaultSceneRootComponent, nullptr));
		}
	}

	for (const FCSAttachmentNode& AttachmentNode : AttachmentNodes)
	{
		if (AttachmentNode.Node->IsRootNode())
		{
			// Node was promoted to root in TryFindOrPromoteRootComponent, skip it
			continue;
		}
		
		// Start by assuming the parent is the actor's root component, then try to find a better match
		FCSRootNodeInfo CurrentParentComponentInfo = ActorRootComponentInfo;
		
		bool bFoundValidNativeParent = false;
		if (const FObjectProperty* ObjectProperty = FindFProperty<FObjectProperty>(Outer, AttachmentNode.AttachToComponentName))
		{
			UClass* ParentClass = ObjectProperty->GetOwnerClass();
			
			if (FCSClassUtilities::IsNativeClass(ParentClass))
			{
				UObject* DefaultObject = ParentClass->GetDefaultObject();
				UObject* Component = ObjectProperty->GetObjectPropertyValue_InContainer(DefaultObject);

				if (IsValid(Component) && Component->IsA<USceneComponent>())
				{
					CurrentParentComponentInfo = FCSRootNodeInfo(ObjectProperty, static_cast<USceneComponent*>(Component));
					bFoundValidNativeParent = true;
				}
			}
		}
		
		if (!bFoundValidNativeParent)
		{
			USCS_Node* ParentNode = GetParentNode(AttachmentNode.AttachToComponentName, Outer, AllNodes);
			
			if (IsValid(ParentNode))
			{
				CurrentParentComponentInfo = FCSRootNodeInfo(ParentNode, CurrentSCS);
			}
		}
		
		USCS_Node* NodeToAttach = AttachmentNode.Node;
		
		if (CurrentParentComponentInfo.IsNative || CurrentParentComponentInfo.IsInOtherSCS)
		{
			NodeToAttach->bIsParentComponentNative = CurrentParentComponentInfo.IsNative;
			NodeToAttach->ParentComponentOrVariableName = CurrentParentComponentInfo.Name;
			NodeToAttach->ParentComponentOwnerClassName = CurrentParentComponentInfo.OwningClass->GetFName();

			// A node is considered a root node if parent is native or in another SCS
			CurrentSCS->AddNode(NodeToAttach);
		}
		else
		{
			USCS_Node* RootNode = GetNodeByName(AllNodes, CurrentParentComponentInfo.Name);
				
			if (!RootNode->ChildNodes.Contains(NodeToAttach))
			{
				bool bAddToAllNodes = !CurrentSCS->GetAllNodes().Contains(NodeToAttach);
				RootNode->AddChildNode(NodeToAttach, bAddToAllNodes);
			}
		}
		
		DetachNodeFromOldParent(NodeToAttach, CurrentSCS, AttachmentNode);
	}
}

USCS_Node* FCSSimpleConstructionScriptCompiler::CreateNode(USimpleConstructionScript* SimpleConstructionScript, UStruct* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName, FString* OptionalName)
{
	FName NodeName = OptionalName ? FName(*OptionalName) : MakeUniqueObjectName(SimpleConstructionScript, USCS_Node::StaticClass());
	USCS_Node* NewNode = NewObject<USCS_Node>(SimpleConstructionScript, NodeName);
	
	NewNode->SetFlags(RF_Transient);
	NewNode->SetVariableName(NewComponentVariableName, false);
	NewNode->VariableGuid = FCSUnrealSharpUtils::ConstructGUIDFromName(NewComponentVariableName);
	
	UpdateTemplateComponent(NewNode, GeneratedClass, NewComponentClass, NewComponentVariableName);

	return NewNode;
}

void FCSSimpleConstructionScriptCompiler::UpdateTemplateComponent(USCS_Node* Node, UStruct* GeneratedClass, UClass* NewComponentClass, FName NewComponentVariableName)
{
	const FName TemplateName(*FString::Printf(TEXT("%s_GEN_VARIABLE"), *NewComponentVariableName.ToString()));

#if WITH_EDITOR
	UActorComponent* OldTemplateObject = IsValid(Node->ComponentTemplate) ? FindObjectFast<UActorComponent>(GeneratedClass, TemplateName) : nullptr;

	if (IsValid(OldTemplateObject))
	{
		OldTemplateObject->Rename(nullptr, GetTransientPackage(), REN_DoNotDirty | REN_DontCreateRedirectors);
	}
#endif
	
	UActorComponent* NewComponentTemplate = NewObject<UActorComponent>(GeneratedClass, NewComponentClass, TemplateName,  RF_ArchetypeObject | RF_Public);
	Node->ComponentClass = NewComponentClass;
	Node->ComponentTemplate = NewComponentTemplate;

#if WITH_EDITOR
	if (ICSManagedTypeInterface* ManagedType = FCSClassUtilities::GetManagedType(NewComponentClass))
	{
		ManagedType->GetManagedReferencesCollection().AddReference(GeneratedClass);
	}
	
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
#endif
}

void FCSSimpleConstructionScriptCompiler::UpdateChildren(UClass* Outer, USCS_Node* Node)
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
		Template->MarkAsGarbage();
		
		Blueprint->InheritableComponentHandler->RemoveOverridenComponentTemplate(ComponentKey);
	}
#endif
}

USCS_Node* FCSSimpleConstructionScriptCompiler::GetParentNode(FName ParentComponentName, const UClass* ClassToSearch, const TArray<FCSNodeInfo>& AllNodes)
{
	USCS_Node* ParentNode = GetNodeByName(AllNodes, ParentComponentName);
	
	if (IsValid(ParentNode))
	{
		return ParentNode;
	}
	
	for (UClass* CurrentClass = ClassToSearch->GetSuperClass(); CurrentClass; CurrentClass = CurrentClass->GetSuperClass())
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
		
		USCS_Node* ParentSCSNode = CurrentSCS->FindSCSNode(ParentComponentName);
		if (!IsValid(ParentSCSNode))
		{
			continue;
		}
		
		ParentNode = ParentSCSNode;
		break;
	}

	return ParentNode;
}

bool FCSSimpleConstructionScriptCompiler::IsRootNode(const TSharedPtr<FCSDefaultComponentType>& DefaultComponentData, const USCS_Node* Node)
{
	if (Node->IsRootNode())
	{
		return true;
	}
	
	if (DefaultComponentData->IsRootComponent)
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

void FCSSimpleConstructionScriptCompiler::ForEachSimpleConstructionScript(USimpleConstructionScript* SimpleConstructionScript, TFunctionRef<bool(USimpleConstructionScript*)> Callback)
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

USCS_Node* FCSSimpleConstructionScriptCompiler::FindRootComponentNode(USimpleConstructionScript* SimpleConstructionScript)
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

void FCSSimpleConstructionScriptCompiler::TryFindOrPromoteRootComponent(USimpleConstructionScript* SimpleConstructionScript, FCSRootNodeInfo& RootComponentNode, UBlueprintGeneratedClass* Outer, const TArray<FCSNodeInfo>& AllNodes)
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
				UObject* ObjectInProperty = Property->GetObjectPropertyValue_InContainer(DefaultActor);
					
				if (ObjectInProperty != RootComponent)
				{
					continue;
				}

				RootComponentNode = FCSRootNodeInfo(Property, RootComponent);
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
			bool HasSocket = NodeInfo.DefaultComponentData->AttachmentSocket != NAME_None;
			bool IsAttached = NodeInfo.DefaultComponentData->AttachmentComponent != NAME_None;
				
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

void FCSSimpleConstructionScriptCompiler::DetachNodeFromOldParent(USCS_Node* Node, USimpleConstructionScript* CurrentSCS, const FCSAttachmentNode& AttachmentNode)
{
#if WITH_EDITOR
	const TArray<USCS_Node*>& AllRegisteredNodes = CurrentSCS->GetAllNodes();
	for (USCS_Node* NodeIterator : AllRegisteredNodes)
	{
		FName ParentComponentName = NodeIterator->GetVariableName();

		if (NodeIterator == Node)
		{
			continue;
		}

		if (AttachmentNode.AttachToComponentName == NAME_None || ParentComponentName != AttachmentNode.AttachToComponentName)
		{
			continue;
		}

		if (NodeIterator->ChildNodes.Contains(Node))
		{
			// The node is already correctly attached
			break;
		}
			
		// The attachment has changed, remove the node from the old parent
		NodeIterator->RemoveChildNode(Node, false);
		break;
	}
#endif
}

USCS_Node* FCSSimpleConstructionScriptCompiler::GetNodeByName(const TArray<FCSNodeInfo>& AllNodes, FName NodeName)
{
	USCS_Node* FoundNode = nullptr;
	
	for (const FCSNodeInfo& NodeInfo : AllNodes)
	{
		if (NodeInfo.Node->GetVariableName() != NodeName)
		{	
			continue;
		}
		
		FoundNode = NodeInfo.Node;
	}

	return FoundNode;
}
