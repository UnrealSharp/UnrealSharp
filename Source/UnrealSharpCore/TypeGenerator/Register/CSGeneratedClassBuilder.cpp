#include "CSGeneratedClassBuilder.h"
#include "CSGeneratedInterfaceBuilder.h"
#include "CSTypeRegistry.h"
#include "UnrealSharpCore/UnrealSharpCore.h"
#include "UnrealSharpCore/TypeGenerator/CSBlueprint.h"
#include "UObject/UnrealType.h"
#include "Engine/Blueprint.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "UnrealSharpCore/TypeGenerator/CSClass.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"
#include "MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Factories/Properties/CSPropertyGenerator.h"

void FCSGeneratedClassBuilder::StartBuildingType()
{
	Field->PurgeClass(false);

	Field->ClassMetaData = FCSTypeRegistry::GetClassInfoFromName(TypeMetaData->Name);
	UClass* SuperClass = FCSTypeRegistry::GetClassFromName(TypeMetaData->ParentClass.Name);

#if WITH_EDITOR
	UBlueprint* EditorBlueprint = static_cast<UBlueprint*>(Field->ClassGeneratedBy);
	EditorBlueprint->ParentClass = SuperClass;
	EditorBlueprint->SkeletonGeneratedClass = Field;
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
	
	// Implement all Blueprint interfaces declared
	ImplementInterfaces(Field, TypeMetaData->Interfaces);
	
	// Generate properties for this class
	FCSPropertyFactory::CreateAndAssignProperties(Field, TypeMetaData->Properties);

	// Generate functions for this class
	FCSFunctionFactory::GenerateVirtualFunctions(Field, TypeMetaData);
	FCSFunctionFactory::GenerateFunctions(Field, TypeMetaData->Functions);

	//Finalize class
	Field->ClassConstructor = &FCSGeneratedClassBuilder::ManagedObjectConstructor;
	
	if (Field->IsChildOf<AActor>())
	{
		SetupDefaultSubobjects(Field->GetClassInfo());
	}
	
	Field->Bind();
	Field->StaticLink(true);
	Field->AssembleReferenceTokenStream();

	//Create the default object for this class
	Field->GetDefaultObject();
	
	SetupTick(Field);
	
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
	
}

FName FCSGeneratedClassBuilder::GetFieldName() const
{
	FString FieldName = FString::Printf(TEXT("%s_C"), *TypeMetaData->Name.ToString());
	return *FieldName;
}

void FCSGeneratedClassBuilder::ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer)
{
	UCSClass* ManagedClass = GetFirstManagedClass(ObjectInitializer.GetClass());
	TSharedPtr<FCSharpClassInfo> ClassInfo = ManagedClass->GetClassInfo();
	
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

void FCSGeneratedClassBuilder::SetupDefaultSubobjects(const TSharedPtr<FCSharpClassInfo>& ClassInfo)
{
	TMap<FObjectProperty*, TSharedPtr<FCSDefaultComponentMetaData>> DefaultComponents;
	TArray<FCSPropertyMetaData>& Properties = ClassInfo->TypeMetaData->Properties;

	USimpleConstructionScript* ConstructionScript = Field->SimpleConstructionScript;
	
    if (ConstructionScript == nullptr)
    {
        ConstructionScript = NewObject<USimpleConstructionScript>(Field);
        ConstructionScript->SetFlags(RF_Transactional);
    	Field->SimpleConstructionScript = ConstructionScript;
    }
	
	const TArray<USCS_Node*>& ExistingNodes = ConstructionScript->GetAllNodes();

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
            ExistingNode = ConstructionScript->CreateNode(PropertyClass, PropertyMetaData.Name);
            ExistingNode->SetVariableName(PropertyMetaData.Name);
        }
    	
        if (DefaultComponent->AttachmentComponent != NAME_None)
        {
            USCS_Node* ParentNode = NodeMap.FindRef(DefaultComponent->AttachmentComponent);
            if (!IsValid(ParentNode))
            {
	            ParentNode = ConstructionScript->GetAllNodes()[0];
            	UE_LOG(LogUnrealSharp, Warning, TEXT("Can't find parent node, attaching to root"));
            }

        	// Try removing the node from its old parent, if the parent is different
        	USCS_Node* OldParentNode = ConstructionScript->FindParentNode(ExistingNode);
        	if (IsValid(OldParentNode) && OldParentNode->GetVariableName() != DefaultComponent->AttachmentComponent)
			{
        		OldParentNode->RemoveChildNode(ExistingNode);
			}
        	
            ParentNode->AddChildNode(ExistingNode);
        }
    }
	
    for (auto It = NodeMap.CreateIterator(); It; ++It)
    {
        if (!ExistingNodes.Contains(It->Value))
        {
            ConstructionScript->AddNode(It->Value);
        }
    }

#if WITH_EDITOR
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
            ConstructionScript->RemoveNode(Node);
        }
    }
#endif

	FName DefaultSceneRoot = USceneComponent::GetDefaultSceneRootVariableName();
	USCS_Node* RootNode = ConstructionScript->FindSCSNode(DefaultSceneRoot);
	
	if (NodeMap.IsEmpty())
	{
		if (!IsValid(RootNode))
		{
			RootNode = ConstructionScript->CreateNode(USceneComponent::StaticClass(), DefaultSceneRoot);
			RootNode->SetVariableName(DefaultSceneRoot);
		}
	}
	else if (IsValid(RootNode))
	{
		ConstructionScript->RemoveNode(RootNode);
	}
}

USCS_Node* FCSGeneratedClassBuilder::CreateNode(UClass* NewComponentClass, FName NewComponentVariableName)
{
	UPackage* TransientPackage = GetTransientPackage();
	UActorComponent* NewComponentTemplate = NewObject<UActorComponent>(TransientPackage, NewComponentClass, NAME_None, RF_ArchetypeObject | RF_Transactional | RF_Public);

#if WITH_EDITOR
	NewComponentTemplate->Modify();
#endif

	FString Name = NewComponentVariableName.ToString() + USimpleConstructionScript::ComponentTemplateNameSuffix;
	UObject* Collision = FindObject<UObject>(Field, *Name);
	while(Collision)
	{
		Collision->Rename(nullptr, GetTransientPackage(), REN_DoNotDirty | REN_DontCreateRedirectors);
		Collision = FindObject<UObject>(Field, *Name);
	}
	
	NewComponentTemplate->Rename(*Name, Field, REN_DoNotDirty | REN_DontCreateRedirectors);

	FName NewComponentTemplateName = MakeUniqueObjectName(Field, NewComponentClass);
	USCS_Node* NewNode = NewObject<USCS_Node>(Field->SimpleConstructionScript, NewComponentTemplateName);
	NewNode->SetFlags(RF_Transactional);
	NewNode->ComponentClass = NewComponentTemplate->GetClass();
	NewNode->ComponentTemplate = NewComponentTemplate;
	NewNode->SetVariableName(NewComponentVariableName, false);

#if WITH_EDITOR
	NewNode->CategoryName = NSLOCTEXT("SCS", "Default", "Default");
	NewNode->VariableGuid = FGuid::NewGuid();
#endif
	
	return NewNode;
}

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

		FImplementedInterface ImplementedInterface;
		ImplementedInterface.Class = InterfaceClass;
		ImplementedInterface.bImplementedByK2 = false;
		ImplementedInterface.PointerOffset = 0;
		ManagedClass->Interfaces.Add(ImplementedInterface);
	}
}

UCSClass* FCSGeneratedClassBuilder::CreateField(UPackage* Package, const FName FieldName)
{
	constexpr EObjectFlags Flags = RF_Public | RF_Standalone | RF_Transactional | RF_LoadCompleted;
	UCSBlueprint* NewBP = NewObject<UCSBlueprint>(Package, UCSBlueprint::StaticClass(), TypeMetaData->Name, Flags);
	UCSClass* Class = TCSGeneratedTypeBuilder::CreateField(Package, FieldName);
#if WITH_EDITOR
	Class->ClassGeneratedBy = NewBP;
#endif
	NewBP->GeneratedClass = Class;;
	return Class;
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