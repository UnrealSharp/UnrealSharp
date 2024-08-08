#include "CSGeneratedClassBuilder.h"
#include "CSGeneratedInterfaceBuilder.h"
#include "CSTypeRegistry.h"
#include "CSharpForUE/CSharpForUE.h"
#include "CSharpForUE/TypeGenerator/CSBlueprint.h"
#include "UObject/UnrealType.h"
#include "Engine/Blueprint.h"
#include "CSharpForUE/TypeGenerator/CSClass.h"
#include "CSharpForUE/TypeGenerator/Factories/CSFunctionFactory.h"
#include "CSharpForUE/TypeGenerator/Factories/CSPropertyFactory.h"
#include "MetaData/CSDefaultComponentMetaData.h"

void FCSGeneratedClassBuilder::StartBuildingType()
{
	//Set the super class for this UClass.
	UClass* SuperClass = FCSTypeRegistry::GetClassFromName(TypeMetaData->ParentClass.Name);
	Field->ClassMetaData = FCSTypeRegistry::GetClassInfoFromName(TypeMetaData->Name);

#if WITH_EDITOR
	FString BlueprintName = TypeMetaData->Name.ToString();
	UBlueprint* DummyBlueprint = FindObject<UBlueprint>(FCSManager::GetUnrealSharpPackage(), *BlueprintName);
	if (!IsValid(DummyBlueprint))
	{
		// Make a dummy blueprint to trick the engine into thinking this class is a blueprint.
		DummyBlueprint = NewObject<UCSBlueprint>(FCSManager::GetUnrealSharpPackage(), *BlueprintName, RF_Public | RF_Standalone | RF_Transactional | RF_LoadCompleted);
	}
	DummyBlueprint->SkeletonGeneratedClass = Field;
	DummyBlueprint->GeneratedClass = Field;
	DummyBlueprint->Status = BS_UpToDate;
	DummyBlueprint->bHasBeenRegenerated = true;
	DummyBlueprint->ParentClass = SuperClass;
	Field->ClassGeneratedBy = DummyBlueprint;
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
	
	//This will only generate functions flagged with BlueprintCallable, BlueprintEvent or virtual functions
	const TSharedRef<FCSClassMetaData> ClassMetaDataRef = TypeMetaData.ToSharedRef();
	FCSFunctionFactory::GenerateVirtualFunctions(Field, ClassMetaDataRef);
	FCSFunctionFactory::GenerateFunctions(Field, ClassMetaDataRef->Functions);
	
	//Generate properties for this class
	FCSPropertyFactory::GeneratePropertiesForType(Field, TypeMetaData->Properties);

	//Finalize class
	if (Field->IsChildOf<AActor>())
	{
		Field->ClassConstructor = &FCSGeneratedClassBuilder::ActorConstructor;
	}
	else if (Field->IsChildOf<UActorComponent>()) 
	{
		// Make all C# ActorComponents BlueprintSpawnableComponent
		#if WITH_EDITOR
		Field->SetMetaData(TEXT("BlueprintSpawnableComponent"), TEXT("true"));
		#endif
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
}

void FCSGeneratedClassBuilder::NewField(UCSClass* OldField, UCSClass* NewField)
{
#if WITH_EDITOR
	// We reuse the Blueprint for the new reinstanced class, so we need to clear the old class references.
	UBlueprint* DummyBlueprint = Cast<UBlueprint>(OldField->ClassGeneratedBy);
	DummyBlueprint->SkeletonGeneratedClass = nullptr;
	DummyBlueprint->GeneratedClass = nullptr;
	DummyBlueprint->ParentClass = nullptr;

	// Since these classes are of UBlueprintGeneratedClass, Unreal considers them in the reinstancing of Blueprints, when a C# class is inheriting from another C# class.
	// We don't want that, so we set the old Blueprint to nullptr. Look ReloadUtilities.cpp:line 166
	// May be a better way? It works so far.
	OldField->ClassGeneratedBy = nullptr;
	OldField->bCooked = true;
#endif
	OldField->ClassFlags |= CLASS_NewerVersionExists;
	FCSTypeRegistry::Get().GetOnNewClassEvent().Broadcast(OldField, NewField);
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
	FCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::ActorComponentConstructor(const FObjectInitializer& ObjectInitializer)
{
	TSharedPtr<FCSharpClassInfo> ClassInfo;
	UCSClass* ManagedClass;
	InitialSetup(ObjectInitializer, ClassInfo, ManagedClass);

	UActorComponent* ActorComponent = static_cast<UActorComponent*>(ObjectInitializer.GetObj());
	ActorComponent->PrimaryComponentTick.bCanEverTick = ManagedClass->bCanTick;
	ActorComponent->PrimaryComponentTick.bStartWithTickEnabled = ManagedClass->bCanTick;
	
	// Make the actual object in C#
	FCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::ActorConstructor(const FObjectInitializer& ObjectInitializer)
{
	TSharedPtr<FCSharpClassInfo> ClassInfo;
	UCSClass* ManagedClass;
	InitialSetup(ObjectInitializer, ClassInfo, ManagedClass);

	AActor* Actor = static_cast<AActor*>(ObjectInitializer.GetObj());
	Actor->PrimaryActorTick.bCanEverTick = ManagedClass->bCanTick;
	Actor->PrimaryActorTick.bStartWithTickEnabled = ManagedClass->bCanTick;
	
	SetupDefaultSubobjects(ObjectInitializer, Actor, ObjectInitializer.GetClass(), ManagedClass, ClassInfo);
	
	// Make the actual object in C#
	FCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::InitialSetup(const FObjectInitializer& ObjectInitializer, TSharedPtr<FCSharpClassInfo>& ClassInfo, UCSClass*& ManagedClass)
{
	ManagedClass = GetFirstManagedClass(ObjectInitializer.GetClass());
	ClassInfo = ManagedClass->GetClassInfo().ToSharedPtr();
	
	//Execute the native class' constructor first.
	UClass* NativeClass = GetFirstNativeClass(ObjectInitializer.GetClass());
	NativeClass->ClassConstructor(ObjectInitializer);
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

	for (const TTuple<FObjectProperty*, TSharedPtr<FCSDefaultComponentMetaData>> DefaultComponent : DefaultComponents)
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

void FCSGeneratedClassBuilder::ImplementInterfaces(UClass* ManagedClass, const TArray<FName>& Interfaces)
{
	for (const FName& InterfaceName : Interfaces)
	{
		UClass* InterfaceClass = FCSTypeRegistry::GetInterfaceFromName(InterfaceName);

		if (!InterfaceClass)
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
