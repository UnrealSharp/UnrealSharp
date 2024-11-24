#include "CSGeneratedClassBuilder.h"
#include "CSGeneratedInterfaceBuilder.h"
#include "CSTypeRegistry.h"
#include "UnrealSharpCore/UnrealSharpCore.h"
#include "UnrealSharpCore/TypeGenerator/CSBlueprint.h"
#include "UObject/UnrealType.h"
#include "Engine/Blueprint.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "Kismet2/BlueprintEditorUtils.h"
#include "Kismet2/KismetEditorUtilities.h"
#include "UnrealSharpCore/TypeGenerator/CSClass.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"
#include "MetaData/CSDefaultComponentMetaData.h"

void FCSGeneratedClassBuilder::StartBuildingType()
{
	//Set the super class for this UClass.
	UClass* SuperClass = FCSTypeRegistry::GetClassFromName(TypeMetaData->ParentClass.Name);
	Field->ClassMetaData = FCSTypeRegistry::GetClassInfoFromName(TypeMetaData->Name);

#if WITH_EDITOR
	UPackage* Package = UCSManager::Get().GetUnrealSharpPackage();
	FString BlueprintName = TypeMetaData->Name.ToString();
	UBlueprint* EditorBlueprint = FindObject<UBlueprint>(Package, *BlueprintName);
	if (!IsValid(EditorBlueprint))
	{
		// Make a dummy blueprint to trick the engine into thinking this class is a blueprint.
		constexpr EObjectFlags Flags = RF_Public | RF_Standalone | RF_Transactional | RF_LoadCompleted;
		EditorBlueprint = NewObject<UCSBlueprint>(Package, *BlueprintName, Flags);
	}
	EditorBlueprint->GeneratedClass = Field;
	EditorBlueprint->ParentClass = SuperClass;
	Field->ClassGeneratedBy = EditorBlueprint;
#endif
	
	Field->ClassFlags = TypeMetaData->ClassFlags | SuperClass->ClassFlags & CLASS_ScriptInherit;

	Field->SetSuperStruct(SuperClass);
	Field->PropertyLink = SuperClass->PropertyLink;
	Field->ClassWithin = SuperClass->ClassWithin;
	Field->ClassCastFlags = SuperClass->ClassCastFlags;

	if (TypeMetaData->ClassConfigName.IsNone())
	{
		Field->ClassConfigName = SuperClass->ClassConfigName;
	}
	else
	{
		Field->ClassConfigName = TypeMetaData->ClassConfigName;
	}
	
	//Implement all Blueprint interfaces declared
	ImplementInterfaces(Field, TypeMetaData->Interfaces);
	
	//Generate properties for this class
#if WITH_EDITOR
	FCSPropertyFactory::CreateAndAssignPropertiesEditor(EditorBlueprint, TypeMetaData->Properties);
#else
	FCSPropertyFactory::CreateAndAssignProperties(Field, TypeMetaData->Properties);
#endif

	//Finalize class
	if (Field->IsChildOf<AActor>())
	{
		Field->ClassConstructor = &FCSGeneratedClassBuilder::ActorConstructor;
#if WITH_EDITOR
		SetupDefaultSubobjectsEditor(Field, Field->GetClassInfo());
#endif
	}
	else if (Field->IsChildOf<UActorComponent>()) 
	{
		Field->ClassConstructor = &FCSGeneratedClassBuilder::ActorComponentConstructor;
	}
	else
	{
		Field->ClassConstructor = &FCSGeneratedClassBuilder::ObjectConstructor;
	}
	
	Field->Bind();
	Field->StaticLink(true);
	Field->AssembleReferenceTokenStream();

	//Create the default object for this class
	Field->GetDefaultObject();
	
	Field->SetUpRuntimeReplicationData();
	Field->UpdateCustomPropertyListForPostConstruction();
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Class);

	if (Field->IsChildOf<UEngineSubsystem>()
#if WITH_EDITOR
	|| Field->IsChildOf<UEditorSubsystem>()
#endif
	)
	{
		FSubsystemCollectionBase::ActivateExternalSubsystem(Field);
	}

#if WITH_EDITOR
	//Compile the blueprint
	CompileClass(EditorBlueprint);
#endif
	
	SetupTick(Field);
}

FString FCSGeneratedClassBuilder::GetFieldName() const
{
	return FString::Printf(TEXT("%s_C"), *TypeMetaData->Name.ToString());
}

void FCSGeneratedClassBuilder::ObjectConstructor(const FObjectInitializer& ObjectInitializer)
{
	TSharedPtr<FCSharpClassInfo> ClassInfo;
	UCSClass* ManagedClass;
	InitialSetup(ObjectInitializer, ClassInfo, ManagedClass);
	
	// Make the actual object in C#
	UCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::ActorComponentConstructor(const FObjectInitializer& ObjectInitializer)
{
	TSharedPtr<FCSharpClassInfo> ClassInfo;
	UCSClass* ManagedClass;
	InitialSetup(ObjectInitializer, ClassInfo, ManagedClass);
	
	// Make the actual object in C#
	UCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::ActorConstructor(const FObjectInitializer& ObjectInitializer)
{
	TSharedPtr<FCSharpClassInfo> ClassInfo;
	UCSClass* ManagedClass;
	InitialSetup(ObjectInitializer, ClassInfo, ManagedClass);
	
	// Make the actual object in C#
	UCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::SetupTick(UCSClass* ManagedClass)
{
	UObject* DefaultObject = ManagedClass->GetDefaultObject();
	
	if (AActor* Actor = Cast<AActor>(DefaultObject))
	{
		Actor->PrimaryActorTick.bCanEverTick = ManagedClass->bCanTick;
		Actor->PrimaryActorTick.bStartWithTickEnabled = ManagedClass->bCanTick;
	}
	else if (UActorComponent* ActorComponent = Cast<UActorComponent>(DefaultObject))
	{
		ActorComponent->PrimaryComponentTick.bCanEverTick = ManagedClass->bCanTick;
		ActorComponent->PrimaryComponentTick.bStartWithTickEnabled = ManagedClass->bCanTick;
	}
}

void FCSGeneratedClassBuilder::CompileClass(UBlueprint* Blueprint)
{
	FBlueprintEditorUtils::MarkBlueprintAsModified(Blueprint);
	FKismetEditorUtilities::CompileBlueprint(Blueprint);
	Blueprint->Modify();
	Blueprint->PostEditChange();
}

void FCSGeneratedClassBuilder::InitialSetup(const FObjectInitializer& ObjectInitializer, TSharedPtr<FCSharpClassInfo>& ClassInfo, UCSClass*& ManagedClass)
{
	ManagedClass = GetFirstManagedClass(ObjectInitializer.GetClass());
	ClassInfo = ManagedClass->GetClassInfo().ToSharedPtr();
	
	//Execute the native class' constructor first.
	UClass* NativeClass = GetFirstNativeClass(ObjectInitializer.GetClass());
	NativeClass->ClassConstructor(ObjectInitializer);

	for (TFieldIterator<FProperty> PropertyIt(ManagedClass); PropertyIt; ++PropertyIt)
	{
		FProperty* Property = *PropertyIt;
		if (!IsManagedType(Property->GetOwnerClass()))
		{
			break;
		}

		if (Property->HasAllPropertyFlags(CPF_ZeroConstructor))
		{
			continue;
		}
		
		Property->InitializeValue_InContainer(ObjectInitializer.GetObj());
	}
}

void FCSGeneratedClassBuilder::SetupDefaultSubobjects(const FObjectInitializer& ObjectInitializer,
                                                      AActor* Actor,
                                                      UClass* ActorClass,
                                                      UCSClass* FirstManagedClass,
                                                      const TSharedPtr<FCSharpClassInfo>& ClassInfo)
{
	if (UCSClass* ManagedClass = Cast<UCSClass>(FirstManagedClass->GetSuperClass()))
	{
		SetupDefaultSubobjects(ObjectInitializer, Actor, ActorClass, ManagedClass, ManagedClass->GetClassInfo());
	}
	
	TMap<FObjectProperty*, TSharedPtr<FCSDefaultComponentMetaData>> DefaultComponents;
	TArray<FCSPropertyMetaData>& Properties = ClassInfo->TypeMetaData->Properties;
	
	for (const FCSPropertyMetaData& PropertyMetaData : Properties)
	{
		if (PropertyMetaData.Type->PropertyType != ECSPropertyType::DefaultComponent)
		{
			continue;
		}

		TSharedPtr<FCSDefaultComponentMetaData> DefaultComponent = StaticCastSharedPtr<FCSDefaultComponentMetaData>(PropertyMetaData.Type);
		FObjectProperty* ObjectProperty = CastField<FObjectProperty>(ActorClass->FindPropertyByName(PropertyMetaData.Name));
		
		UObject* NewSubObject = ObjectInitializer.CreateDefaultSubobject(Actor, ObjectProperty->GetFName(), ObjectProperty->PropertyClass, ObjectProperty->PropertyClass, true, false);
		ObjectProperty->SetObjectPropertyValue_InContainer(Actor, NewSubObject);
		DefaultComponents.Add(ObjectProperty, DefaultComponent);
	}

	for (const TTuple<FObjectProperty*, TSharedPtr<FCSDefaultComponentMetaData>>& DefaultComponent : DefaultComponents)
	{
		FObjectProperty* Property = DefaultComponent.Key;
		TSharedPtr<FCSDefaultComponentMetaData> DefaultComponentMetaData = DefaultComponent.Value;
		
		USceneComponent* SceneComponent = Cast<USceneComponent>(Property->GetObjectPropertyValue_InContainer(Actor));
		
		if (!SceneComponent)
		{
			continue;
		}

		if (!Actor->GetRootComponent() && DefaultComponentMetaData->IsRootComponent)
		{
			Actor->SetRootComponent(SceneComponent);
			continue;
		}

		FName AttachmentComponentName = DefaultComponentMetaData->AttachmentComponent;
		FName AttachmentSocketName = DefaultComponentMetaData->AttachmentSocket;

		if (FObjectProperty* ObjectProperty = FindFProperty<FObjectProperty>(Actor->GetClass(), *AttachmentComponentName.ToString(), EFieldIterationFlags::IncludeSuper))
		{
			USceneComponent* AttachmentComponent = Cast<USceneComponent>(ObjectProperty->GetObjectPropertyValue_InContainer(Actor));
			if (IsValid(AttachmentComponent))
			{
				SceneComponent->SetupAttachment(AttachmentComponent, AttachmentSocketName.IsNone() ? NAME_None : AttachmentSocketName);
				continue;
			}
		}
		
		SceneComponent->SetupAttachment(Actor->GetRootComponent());
	}
}

#if WITH_EDITOR
void FCSGeneratedClassBuilder::SetupDefaultSubobjectsEditor(UClass* ActorClass, const TSharedPtr<FCSharpClassInfo>& ClassInfo)
{
	UBlueprint* Blueprint = Cast<UBlueprint>(ActorClass->ClassGeneratedBy);
	TMap<FObjectProperty*, TSharedPtr<FCSDefaultComponentMetaData>> DefaultComponents;
	TArray<FCSPropertyMetaData>& Properties = ClassInfo->TypeMetaData->Properties;

    if (Blueprint->SimpleConstructionScript == nullptr)
    {
        Blueprint->SimpleConstructionScript = NewObject<USimpleConstructionScript>(ActorClass);
        Blueprint->SimpleConstructionScript->SetFlags(RF_Transactional);
    }
	
	const TArray<USCS_Node*>& ExistingNodes = Blueprint->SimpleConstructionScript->GetAllNodes();

	TMap<FName, USCS_Node*> NodeMap;
    for (USCS_Node* Node : ExistingNodes)
    {
        NodeMap.Add(Node->GetVariableName(), Node);
    }

    for (const FCSPropertyMetaData& PropertyMetaData : Properties)
    {
        if (PropertyMetaData.Type->PropertyType != ECSPropertyType::DefaultComponent)
        {
            continue;
        }

        TSharedPtr<FCSDefaultComponentMetaData> DefaultComponent = StaticCastSharedPtr<FCSDefaultComponentMetaData>(PropertyMetaData.Type);
        UClass* PropertyClass = FCSTypeRegistry::GetClassFromName(DefaultComponent->InnerType.Name);
    	
        USCS_Node*& ExistingNode = NodeMap.FindOrAdd(PropertyMetaData.Name);
        if (!ExistingNode || ExistingNode->ComponentClass != PropertyClass)
        {
            ExistingNode = Blueprint->SimpleConstructionScript->CreateNode(PropertyClass, PropertyMetaData.Name);
            ExistingNode->ParentComponentOrVariableName = PropertyMetaData.Name;
            ExistingNode->SetVariableName(PropertyMetaData.Name);
        }
    	
        if (DefaultComponent->AttachmentComponent != NAME_None)
        {
            USCS_Node* ParentNode = NodeMap.FindRef(DefaultComponent->AttachmentComponent);
            if (!ParentNode)
            {
	            ParentNode = Blueprint->SimpleConstructionScript->GetAllNodes()[0];
            	UE_LOG(LogUnrealSharp, Warning, TEXT("Can't find parent node, attaching to root"));
            }

        	auto TryRemoveFromOldParent = [ExistingNode, ExistingNodes, DefaultComponent]()
        	{
        		for (USCS_Node* Node : ExistingNodes)
        		{
        			const TArray<USCS_Node*>& ChildNodes = Node->GetChildNodes();
        			for (int32 i = ChildNodes.Num() - 1; i >= 0; --i)
        			{
        				USCS_Node* ChildNode = ChildNodes[i];
        				if (ChildNode == ExistingNode && Node->GetVariableName() != DefaultComponent->AttachmentComponent)
        				{
        					Node->RemoveChildNode(ChildNode);
        					return;
        				}
        			}
        		}
        	};

        	TryRemoveFromOldParent();
            ParentNode->AddChildNode(ExistingNode);
        }
    }
	
    for (auto It = NodeMap.CreateIterator(); It; ++It)
    {
        if (!ExistingNodes.Contains(It->Value))
        {
            Blueprint->SimpleConstructionScript->AddNode(It->Value);
        }
    }
	
    for (int32 i = ExistingNodes.Num() - 1; i >= 0; --i)
    {
        USCS_Node* Node = ExistingNodes[i];
        bool bShouldBeRemoved = true;

        for (const FCSPropertyMetaData& PropertyMetaData : Properties)
        {
            if (Node->GetVariableName() == PropertyMetaData.Name)
            {
                bShouldBeRemoved = false;
                break;
            }
        }

        if (bShouldBeRemoved)
        {
            Blueprint->SimpleConstructionScript->RemoveNode(Node);
        }
    }
}
#endif

void FCSGeneratedClassBuilder::ImplementInterfaces(UClass* ManagedClass, const TArray<FName>& Interfaces)
{
	for (const FName& InterfaceName : Interfaces)
	{
		UClass* InterfaceClass = FCSTypeRegistry::GetInterfaceFromName(InterfaceName);

		if (!IsValid(InterfaceClass))
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Can't find interface: %s"), *InterfaceName.ToString());
			continue;
		}

#if WITH_EDITOR
		UBlueprint* Blueprint = static_cast<UBlueprint*>(ManagedClass->ClassGeneratedBy);
		FBPInterfaceDescription InterfaceDescription;
		InterfaceDescription.Interface = InterfaceClass;
		Blueprint->ImplementedInterfaces.Add(InterfaceDescription);
#else
		FImplementedInterface ImplementedInterface;
		ImplementedInterface.Class = InterfaceClass;
		ImplementedInterface.bImplementedByK2 = false;
		ImplementedInterface.PointerOffset = 0;
		ManagedClass->Interfaces.Add(ImplementedInterface);
#endif
	}
}

void* FCSGeneratedClassBuilder::TryGetManagedFunction(UClass* Outer, const FName& MethodName)
{
	if (UCSClass* ManagedClass = GetFirstManagedClass(Outer))
	{
		const FString InvokeMethodName = FString::Printf(TEXT("Invoke_%s"), *MethodName.ToString());
		return FCSManagedCallbacks::ManagedCallbacks.LookupManagedMethod(ManagedClass->GetClassInfo()->TypeHandle, *InvokeMethodName);
	}
	return nullptr;
}

UCSClass* FCSGeneratedClassBuilder::GetFirstManagedClass(UClass* Class)
{
	while (Class && !IsManagedType(Class))
	{
		Class = Class->GetSuperClass();
	}
	return Cast<UCSClass>(Class);
}

UClass* FCSGeneratedClassBuilder::GetFirstNativeClass(UClass* Class)
{
	while (!Class->HasAnyClassFlags(CLASS_Native))
	{
		Class = Class->GetSuperClass();
	}
	return Class;
}

UClass* FCSGeneratedClassBuilder::GetFirstNonBlueprintClass(UClass* Class)
{
	while (Class->HasAnyClassFlags(CLASS_CompiledFromBlueprint))
	{
		Class = Class->GetSuperClass();
	}
	return Class;
}

bool FCSGeneratedClassBuilder::IsManagedType(const UClass* Class)
{
	return Class->GetClass() == UCSClass::StaticClass(); 
}
