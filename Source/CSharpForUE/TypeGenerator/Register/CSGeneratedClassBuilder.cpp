#include "CSGeneratedClassBuilder.h"
#include "CSGeneratedInterfaceBuilder.h"
#include "CSharpForUE/CSharpForUE.h"
#include "CSharpForUE/TypeGenerator/CSBlueprint.h"
#include "UObject/UnrealType.h"
#include "Engine/Blueprint.h"
#include "CSharpForUE/TypeGenerator/CSClass.h"
#include "CSharpForUE/TypeGenerator/Factories/CSFunctionFactory.h"
#include "CSharpForUE/TypeGenerator/Factories/CSPropertyFactory.h"

void FCSGeneratedClassBuilder::StartBuildingType()
{
	//Set the super class for this UClass.
	{
		UClass* SuperClass = FCSTypeRegistry::GetClassFromName(*TypeMetaData->ParentClass.Name);

		// Make a dummy blueprint to trick the engine into thinking this class is a blueprint.
		{
			UBlueprint* DummyBlueprint = NewObject<UCSBlueprint>(Field, Field->GetFName(), RF_Public | RF_Standalone | RF_Transactional | RF_LoadCompleted);
			DummyBlueprint->SkeletonGeneratedClass = Field;
			DummyBlueprint->GeneratedClass = Field;
			DummyBlueprint->ParentClass = SuperClass;

			#if WITH_EDITOR
			DummyBlueprint->BlueprintDisplayName = Field->GetName();
			Field->ClassGeneratedBy = DummyBlueprint;
			#endif
		}
		
		Field->ClassFlags = TypeMetaData->ClassFlags;
		Field->SetSuperStruct(SuperClass);
		Field->PropertyLink = SuperClass->PropertyLink;
		Field->ClassWithin = SuperClass->ClassWithin;

		if (TypeMetaData->ClassConfigName.IsEmpty())
		{
			Field->ClassConfigName = SuperClass->ClassConfigName;
		}
		else
		{
			Field->ClassConfigName = *TypeMetaData->ClassConfigName;
		}
	}

	//Implement all Blueprint interfaces declared
	{
		ImplementInterfaces(Field, TypeMetaData->Interfaces);
	}
	
	//This will only generate functions flagged with BlueprintCallable, BlueprintEvent or virtual functions
	{
		const TSharedRef<FClassMetaData> ClassMetaDataRef = TypeMetaData.ToSharedRef();
		FCSFunctionFactory::GenerateVirtualFunctions(Field, ClassMetaDataRef);
		FCSFunctionFactory::GenerateFunctions(Field, ClassMetaDataRef->Functions);
	}
	
	//Generate properties for this class
	{
		FCSPropertyFactory::GeneratePropertiesForType(Field, TypeMetaData->Properties);
	}

	//Finalize class
	{
		if (Field->IsChildOf<AActor>())
		{
			Field->ClassConstructor = &FCSGeneratedClassBuilder::ActorConstructor;
		}
		else
		{
			#if WITH_EDITOR
			// Make all C# ActorComponents BlueprintSpawnableComponent
			if (Field->IsChildOf<UActorComponent>())
			{
				Field->SetMetaData(TEXT("BlueprintSpawnableComponent"), TEXT("true"));
			}
			#endif
			
			Field->ClassConstructor = &FCSGeneratedClassBuilder::ObjectConstructor;
		}
		
		Field->Bind();
		Field->StaticLink(true);
		Field->AssembleReferenceTokenStream();

		//Create the default object for this class
		Field->GetDefaultObject();

		// Setup replication properties
		Field->SetUpRuntimeReplicationData();

		Field->UpdateCustomPropertyListForPostConstruction();
		
		RegisterFieldToLoader(ENotifyRegistrationType::NRT_Class);
	}
}

void FCSGeneratedClassBuilder::NewField(UCSClass* OldField, UCSClass* NewField)
{
	FCSTypeRegistry::Get().GetOnNewClassEvent().Broadcast(OldField, NewField);
}

void FCSGeneratedClassBuilder::ObjectConstructor(const FObjectInitializer& ObjectInitializer)
{
	UClass* Class = ObjectInitializer.GetClass();
	UCSClass* ManagedClass = GetFirstManagedClass(Class);
	const FCSharpClassInfo* ClassInfo = FCSTypeRegistry::Get().FindManagedType(ManagedClass);
	
	//Execute the native class' constructor first.
	UClass* NativeClass = GetFirstNativeClass(Class);
	NativeClass->ClassConstructor(ObjectInitializer);
	
	// Make the actual object in C#
	FCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::ActorConstructor(const FObjectInitializer& ObjectInitializer)
{
	UClass* Class = ObjectInitializer.GetClass();
	UCSClass* ManagedClass = GetFirstManagedClass(Class);
	const FCSharpClassInfo* ClassInfo = FCSTypeRegistry::Get().FindManagedType(ManagedClass);
	
	//Execute the native class' constructor first.
	UClass* NativeClass = GetFirstNativeClass(Class);
	NativeClass->ClassConstructor(ObjectInitializer);
	
	AActor* Actor = CastChecked<AActor>(ObjectInitializer.GetObj());
	Actor->PrimaryActorTick.bCanEverTick = ManagedClass->bCanTick;
	Actor->PrimaryActorTick.bStartWithTickEnabled = ManagedClass->bCanTick;
	SetupDefaultSubobjects(ObjectInitializer, Actor, Class, ClassInfo->TypeMetaData);
	
	// Make the actual object in C#
	FCSManager::Get().CreateNewManagedObject(ObjectInitializer.GetObj(), ClassInfo->TypeHandle);
}

void FCSGeneratedClassBuilder::SetupDefaultSubobjects(const FObjectInitializer& ObjectInitializer, AActor* Actor, const UClass* ActorClass, const TSharedPtr<FClassMetaData>& ClassMetaData)
{
	TMap<FObjectProperty*, TSharedPtr<FDefaultComponentMetaData>> DefaultComponents;
	
	for (const FPropertyMetaData& PropertyMetaData : ClassMetaData->Properties)
	{
		if (PropertyMetaData.Type->PropertyType != ECSPropertyType::DefaultComponent)
		{
			continue;
		}

		TSharedPtr<FDefaultComponentMetaData> DefaultComponent = StaticCastSharedPtr<FDefaultComponentMetaData>(PropertyMetaData.Type);
		FObjectProperty* ObjectProperty = CastField<FObjectProperty>(ActorClass->FindPropertyByName(PropertyMetaData.Name));
		
		UObject* NewSubObject = ObjectInitializer.CreateDefaultSubobject(Actor, ObjectProperty->GetFName(), ObjectProperty->PropertyClass, ObjectProperty->PropertyClass, true, false);
		ObjectProperty->SetObjectPropertyValue_InContainer(Actor, NewSubObject);
		DefaultComponents.Add(ObjectProperty, DefaultComponent);
	}

	for (const TTuple<FObjectProperty*, TSharedPtr<FDefaultComponentMetaData>> DefaultComponent : DefaultComponents)
	{
		FObjectProperty* Property = DefaultComponent.Key;
		TSharedPtr<FDefaultComponentMetaData> DefaultComponentMetaData = DefaultComponent.Value;
		
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

		FString AttachmentComponentName = DefaultComponentMetaData->AttachmentComponent;
		FString AttachmentSocketName = DefaultComponentMetaData->AttachmentSocket;
    	
		USceneComponent* AttachmentComponent = !AttachmentComponentName.IsEmpty() ? FindObject<USceneComponent>(Actor, *AttachmentComponentName) : Actor->GetRootComponent();

		if (AttachmentComponent)
		{
			SceneComponent->SetupAttachment(AttachmentComponent, AttachmentSocketName.IsEmpty() ? NAME_None : FName(*AttachmentSocketName));
		}
		else
		{
			SceneComponent->SetupAttachment(Actor->GetRootComponent());
		}
	}
}

void FCSGeneratedClassBuilder::ImplementInterfaces(UClass* ManagedClass, const TArray<FString>& Interfaces)
{
	for (const FString& InterfaceName : Interfaces)
	{
		UClass* InterfaceClass = FCSTypeRegistry::GetInterfaceFromName(*InterfaceName);

		if (!InterfaceClass)
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Can't find interface: %s"), *InterfaceName);
			continue;
		}

		FImplementedInterface ImplementedInterface;
		ImplementedInterface.Class = InterfaceClass;
		ImplementedInterface.bImplementedByK2 = false;
		ImplementedInterface.PointerOffset = 0;

		ManagedClass->Interfaces.Add(ImplementedInterface);
	}
}

void* FCSGeneratedClassBuilder::TryGetManagedFunction(const UClass* Outer, const FName& MethodName)
{
	const FCSharpClassInfo* ClassInfo = FCSTypeRegistry::GetClassInfoFromName(Outer->GetFName());

	if (!ClassInfo)
	{
		return nullptr;
	}
	
	const FString InvokeMethodName = FString::Printf(TEXT("Invoke_%s"), *MethodName.ToString());
	return FCSManagedCallbacks::ManagedCallbacks.LookupManagedMethod(ClassInfo->TypeHandle, *InvokeMethodName);
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
