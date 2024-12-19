#include "CSGeneratedClassBuilder.h"
#include "CSGeneratedInterfaceBuilder.h"
#include "CSTypeRegistry.h"
#include "BehaviorTree/Tasks/BTTask_BlueprintBase.h"
#include "UnrealSharpCore/UnrealSharpCore.h"
#include "UnrealSharpCore/TypeGenerator/CSBlueprint.h"
#include "UObject/UnrealType.h"
#include "Engine/Blueprint.h"
#include "UnrealSharpCore/TypeGenerator/CSClass.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"
#include "MetaData/CSDefaultComponentMetaData.h"

void FCSGeneratedClassBuilder::StartBuildingType()
{
	//Set the super class for this UClass.
	UClass* SuperClass = FCSTypeRegistry::GetClassFromName(TypeMetaData->ParentClass.Name);
	Field->SetClassMetaData(FCSTypeRegistry::GetClassInfoFromName(TypeMetaData->Name));

#if WITH_EDITOR
	UPackage* Package = UCSManager::Get().GetUnrealSharpPackage();
	FString BlueprintName = TypeMetaData->Name.ToString();
	UBlueprint* DummyBlueprint = FindObject<UBlueprint>(Package, *BlueprintName);
	if (!IsValid(DummyBlueprint))
	{
		// Make a dummy blueprint to trick the engine into thinking this class is a blueprint.
		DummyBlueprint = NewObject<UCSBlueprint>(Package, *BlueprintName, RF_Public | RF_Standalone);
	}
	// Needed for the BP-callable functions to show up in the blueprint editor.
	DummyBlueprint->SkeletonGeneratedClass = Field;
	DummyBlueprint->GeneratedClass = Field;
	DummyBlueprint->Status = BS_UpToDate;
	DummyBlueprint->bHasBeenRegenerated = true;
	DummyBlueprint->ParentClass = SuperClass;
	Field->ClassGeneratedBy = DummyBlueprint;
#endif

	Field->SetSuperStruct(SuperClass);

	Field->ClassFlags = TypeMetaData->ClassFlags | SuperClass->ClassFlags & CLASS_ScriptInherit;

	// If this is a blueprint task, mark it as native, otherwise it will not be able to be used in the behavior tree.
	if (Field->IsChildOf(UBTTask_BlueprintBase::StaticClass()))
	{
		Field->ClassFlags |= CLASS_Native;
	}

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
	if (Field->IsChildOf<AActor>())
	{
		Field->ClassConstructor = &FCSGeneratedClassBuilder::ManagedActorConstructor;
	}
	else
	{
		Field->ClassConstructor = &FCSGeneratedClassBuilder::ManagedObjectConstructor;
	}

	Field->Bind();
	Field->StaticLink(true);
	Field->AssembleReferenceTokenStream();

	//Create the default object for this class
	UObject* DefaultObject = Field->GetDefaultObject();
	SetupDefaultTickSettings(DefaultObject);

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

#if WITH_EDITOR
void FCSGeneratedClassBuilder::OnFieldReplaced(UCSClass* OldField, UCSClass* NewField)
{
	// We reuse the Blueprint for the new reinstanced class, so we need to clear the old class references.
	UBlueprint* DummyBlueprint = Cast<UBlueprint>(OldField->ClassGeneratedBy);
	DummyBlueprint->SkeletonGeneratedClass = nullptr;
	DummyBlueprint->GeneratedClass = nullptr;
	DummyBlueprint->ParentClass = nullptr;

	OldField->ClassFlags |= CLASS_NewerVersionExists;
	
	// Since these classes are of UBlueprintGeneratedClass, Unreal considers them in the reinstancing of Blueprints, when a C# class is inheriting from another C# class.
	// We don't want that, so we set the old Blueprint to nullptr. Look ReloadUtilities.cpp:line 166
	// May be a better way? It works so far.
	OldField->ClassGeneratedBy = nullptr;
	OldField->bCooked = true;

	if (Field->IsChildOf<UEngineSubsystem>() || Field->IsChildOf<UEditorSubsystem>())
	{
		FSubsystemCollectionBase::DeactivateExternalSubsystem(OldField);
	}

	FCSTypeRegistry::Get().GetOnNewClassEvent().Broadcast(OldField, NewField);
}
#endif

void FCSGeneratedClassBuilder::ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer)
{
	UCSClass* ManagedClass;
	TSharedPtr<const FCSharpClassInfo> ClassInfo;
	InitialSetup(ObjectInitializer, ManagedClass, ClassInfo);

	UCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::ManagedActorConstructor(const FObjectInitializer& ObjectInitializer)
{
	UCSClass* ManagedClass;
	TSharedPtr<const FCSharpClassInfo> ClassInfo;
	InitialSetup(ObjectInitializer, ManagedClass, ClassInfo);

	AActor* Actor = static_cast<AActor*>(ObjectInitializer.GetObj());
	SetupDefaultSubobjects(ObjectInitializer, Actor, Actor->GetClass(), ManagedClass, ClassInfo);

	UCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::InitialSetup(const FObjectInitializer& ObjectInitializer, UCSClass*& OutManagedClass,
                                            TSharedPtr<const FCSharpClassInfo>& OutClassInfo)
{
	OutManagedClass = GetFirstManagedClass(ObjectInitializer.GetClass());
	OutClassInfo = OutManagedClass->GetClassInfo();

	//Execute the native class' constructor first.
	UClass* NativeClass = GetFirstNativeClass(ObjectInitializer.GetClass());
	NativeClass->ClassConstructor(ObjectInitializer);

	// Initialize properties that are not zero initialized such as FText.
	for (TFieldIterator<FProperty> PropertyIt(OutManagedClass); PropertyIt; ++PropertyIt)
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


void FCSGeneratedClassBuilder::SetupDefaultTickSettings(UObject* DefaultObject) const
{
	if (AActor* Actor = Cast<AActor>(DefaultObject))
	{
		Actor->PrimaryActorTick.bCanEverTick = Field->CanTick();
		Actor->PrimaryActorTick.bStartWithTickEnabled = Field->CanTick();
	}
	else if (UActorComponent* ActorComponent = Cast<UActorComponent>(DefaultObject))
	{
		ActorComponent->PrimaryComponentTick.bCanEverTick = Field->CanTick();
		ActorComponent->PrimaryComponentTick.bStartWithTickEnabled = Field->CanTick();
	}
}

void FCSGeneratedClassBuilder::SetupDefaultSubobjects(const FObjectInitializer& ObjectInitializer,
                                                      AActor* Actor,
                                                      UClass* ActorClass,
                                                      UCSClass* FirstManagedClass,
                                                      const TSharedPtr<const FCSharpClassInfo>& ClassInfo)
{
	
	if (UCSClass* ManagedClass = Cast<UCSClass>(FirstManagedClass->GetSuperClass()))
	{
		SetupDefaultSubobjects(ObjectInitializer, Actor, ActorClass, ManagedClass,
		                                           ManagedClass->GetClassInfo());
	}

	TMap<FObjectProperty*, TSharedPtr<FCSDefaultComponentMetaData>> DefaultComponents;
	TArray<FCSPropertyMetaData>& Properties = ClassInfo->TypeMetaData->Properties;

	for (const FCSPropertyMetaData& PropertyMetaData : Properties)
	{
		if (PropertyMetaData.Type->PropertyType != ECSPropertyType::DefaultComponent)
		{
			continue;
		}

		TSharedPtr<FCSDefaultComponentMetaData> DefaultComponent = StaticCastSharedPtr<FCSDefaultComponentMetaData>(
			PropertyMetaData.Type);
		FObjectProperty* ObjectProperty = CastField<FObjectProperty>(
			ActorClass->FindPropertyByName(PropertyMetaData.Name));

		UObject* NewSubObject = ObjectInitializer.CreateDefaultSubobject(
			Actor, ObjectProperty->GetFName(), ObjectProperty->PropertyClass, ObjectProperty->PropertyClass, true,
			false);
		
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

		if (FObjectProperty* ObjectProperty = FindFProperty<FObjectProperty>(
			Actor->GetClass(), *AttachmentComponentName.ToString(), EFieldIterationFlags::IncludeSuper))
		{
			USceneComponent* AttachmentComponent = Cast<USceneComponent>(
				ObjectProperty->GetObjectPropertyValue_InContainer(Actor));

			if (IsValid(AttachmentComponent) && AttachmentComponent != SceneComponent)
			{
				FName Socket = AttachmentSocketName.IsNone() ? NAME_None : AttachmentSocketName;

				// Less great. BP somehow serialize the old attachment component even if we attach it to a new one, so we need to reattach it.
				// This is a workaround for now until I have sanity to fix this properly.
				{
					UObject* Archetype = Actor->GetArchetype();
					USceneComponent* Template = Cast<USceneComponent>(
						Archetype->GetDefaultSubobjectByName(DefaultComponent.Key->GetFName()));
					USceneComponent* TemplateAttachmentComponent = Cast<USceneComponent>(
						Archetype->GetDefaultSubobjectByName(AttachmentComponentName));

					if (IsValid(Template) && IsValid(TemplateAttachmentComponent) && Template->GetAttachParent() !=
						TemplateAttachmentComponent)
					{
						Template->SetupAttachment(TemplateAttachmentComponent, Socket);
					}
				}
				
				SceneComponent->SetupAttachment(AttachmentComponent, Socket);
				continue;
			}
		}

		SceneComponent->SetupAttachment(Actor->GetRootComponent());
	}
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
	while (!Class->HasAnyClassFlags(CLASS_Native) || IsManagedType(Class))
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
