#include "CSGeneratedClassBuilder.h"

#include "CSAssembly.h"
#include "CSGeneratedInterfaceBuilder.h"
#include "CSManager.h"
#include "CSMetaDataUtils.h"
#include "CSSimpleConstructionScriptBuilder.h"
#include "UnrealSharpCore/UnrealSharpCore.h"
#include "UnrealSharpCore/TypeGenerator/CSBlueprint.h"
#include "UObject/UnrealType.h"
#include "Engine/Blueprint.h"
#include "Extensions/DeveloperSettings/CSDeveloperSettings.h"
#include "TypeGenerator/CSSkeletonClass.h"
#include "TypeInfo/CSClassInfo.h"
#include "UnrealSharpCore/TypeGenerator/CSClass.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"
#include "Utils/CSClassUtilities.h"

FCSGeneratedClassBuilder::FCSGeneratedClassBuilder(const TSharedPtr<FCSClassMetaData>& InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly): TCSGeneratedTypeBuilder(InTypeMetaData, InOwningAssembly)
{
	RedirectClasses.Add(UDeveloperSettings::StaticClass(), UCSDeveloperSettings::StaticClass());
}

void FCSGeneratedClassBuilder::RebuildType()
{
	if (!Field->HasTypeInfo())
	{
		TSharedPtr<FCSClassInfo> ClassInfo = OwningAssembly->FindClassInfo(TypeMetaData->FieldName);
		Field->SetTypeInfo(ClassInfo);
	}

	UClass* CurrentSuperClass = Field->GetSuperClass();
	if (!IsValid(CurrentSuperClass) || CurrentSuperClass->GetFName() != TypeMetaData->ParentClass.FieldName.GetName())
	{
		UClass* SuperClass = TypeMetaData->ParentClass.GetOwningClass();
		if (UClass** RedirectedClass = RedirectClasses.Find(SuperClass))
		{
			SuperClass = *RedirectedClass;
		}

		CurrentSuperClass = SuperClass;
	}
	
	Field->SetSuperStruct(CurrentSuperClass);

	// Reset for each rebuild of the class, so it doesn't accumulate properties from previous builds.
	Field->NumReplicatedProperties = 0;
	
#if WITH_EDITOR
	if (FUnrealSharpUtils::IsStandalonePIE())
	{
		// Since the BP-compiler is not present in standalone, we just do a normal class creation like in a packaged game.
		// Some things still reference the Blueprint in standalone, so we need to create it.
		CreateBlueprint(CurrentSuperClass);
		CreateClass(CurrentSuperClass);
	}
	else
	{
		CreateClassEditor(CurrentSuperClass);
	}
#else
	CreateClass(CurrentSuperClass);
#endif
}

#if WITH_EDITOR
void FCSGeneratedClassBuilder::UpdateType()
{
	UCSManager::Get().OnClassReloadedEvent().Broadcast(Field);
}

void FCSGeneratedClassBuilder::CreateClassEditor(UClass* SuperClass)
{
	CreateBlueprint(SuperClass);
	UCSManager::Get().OnNewClassEvent().Broadcast(Field);
}

void FCSGeneratedClassBuilder::CreateBlueprint(UClass* SuperClass)
{
	UBlueprint* Blueprint = static_cast<UBlueprint*>(Field->ClassGeneratedBy);
	if (!Blueprint)
	{
		UPackage* Package = TypeMetaData->GetOwningPackage();
		FName BlueprintName = FCSMetaDataUtils::GetAdjustedFieldName(TypeMetaData->FieldName);
		
		Blueprint = NewObject<UCSBlueprint>(Package, *BlueprintName.ToString(), RF_Public | RF_Standalone);
		Blueprint->GeneratedClass = Field;
		Blueprint->ParentClass = SuperClass;
		
		Field->ClassGeneratedBy = Blueprint;
	}
}
#endif

void FCSGeneratedClassBuilder::CreateClass(UClass* SuperClass)
{
	Field->ClassFlags = TypeMetaData->ClassFlags | SuperClass->ClassFlags & CLASS_ScriptInherit;

	Field->PropertyLink = SuperClass->PropertyLink;
	Field->ClassWithin = SuperClass->ClassWithin;
	Field->ClassCastFlags = SuperClass->ClassCastFlags;

	SetConfigName(Field, TypeMetaData);

	// Implement all Blueprint interfaces declared
	ImplementInterfaces(Field, TypeMetaData->Interfaces);

	// Generate properties for this class
	FCSPropertyFactory::CreateAndAssignProperties(Field, TypeMetaData->Properties);

	// Build the construction script that will spawn the components
	FCSSimpleConstructionScriptBuilder::BuildSimpleConstructionScript(Field, &Field->SimpleConstructionScript, TypeMetaData->Properties);

	// Generate functions for this class
	FCSFunctionFactory::GenerateVirtualFunctions(Field, TypeMetaData);
	FCSFunctionFactory::GenerateFunctions(Field, TypeMetaData->Functions);

	//Finalize class
	Field->ClassConstructor = &FCSGeneratedClassBuilder::ManagedObjectConstructor;

	Field->Bind();
	Field->StaticLink(true);
	Field->AssembleReferenceTokenStream();

	//Create the default object for this class
	UObject* DefaultObject = Field->GetDefaultObject();
	SetupDefaultTickSettings(DefaultObject, Field);

	Field->SetUpRuntimeReplicationData();
	Field->UpdateCustomPropertyListForPostConstruction();

	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Class);
	TryRegisterSubsystem(Field);
}

FName FCSGeneratedClassBuilder::GetFieldName() const
{
	// Blueprint classes have a _C suffix
	FString FieldName = TCSGeneratedTypeBuilder<FCSClassMetaData, UCSClass>::GetFieldName().ToString();
	FieldName += TEXT("_C");
	
	return *FieldName;
}

void FCSGeneratedClassBuilder::ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer)
{
	UCSClass* FirstManagedClass = FCSClassUtilities::GetFirstManagedClass(ObjectInitializer.GetClass());
	UClass* FirstNativeClass = FCSClassUtilities::GetFirstNativeClass(FirstManagedClass);
	
	//Execute the native class' constructor first.
	FirstNativeClass->ClassConstructor(ObjectInitializer);

	// Initialize managed properties that are not zero initialized such as FText.
	for (TFieldIterator<FProperty> PropertyIt(FirstManagedClass); PropertyIt; ++PropertyIt)
	{
		FProperty* Property = *PropertyIt;

		if (!FCSClassUtilities::IsManagedType(Property->GetOwnerClass()))
		{
			// We don't want to initialize properties that are not from a managed class
			break;
		}
		
		if (Property->HasAnyPropertyFlags(CPF_ZeroConstructor))
		{
			continue;
		}

		Property->InitializeValue_InContainer(ObjectInitializer.GetObj());
	}

	TSharedPtr<FCSAssembly> OwningAssembly = FirstManagedClass->GetTypeInfo()->OwningAssembly;
	OwningAssembly->CreateManagedObject(ObjectInitializer.GetObj());
}

void FCSGeneratedClassBuilder::SetupDefaultTickSettings(UObject* DefaultObject, const UClass* Class)
{
	FTickFunction* TickFunction;
	FTickFunction* ParentTickFunction;
	if (AActor* Actor = Cast<AActor>(DefaultObject))
	{
		TickFunction = &Actor->PrimaryActorTick;
		ParentTickFunction = &Class->GetSuperClass()->GetDefaultObject<AActor>()->PrimaryActorTick;
	}
	else if (UActorComponent* Component = Cast<UActorComponent>(DefaultObject))
	{
		TickFunction = &Component->PrimaryComponentTick;
		ParentTickFunction = &Class->GetSuperClass()->GetDefaultObject<UActorComponent>()->PrimaryComponentTick;
	}
	else
	{
		return;
	}
	
	TickFunction->bCanEverTick = ParentTickFunction->bCanEverTick;
	if (TickFunction->bCanEverTick)
	{
		return;
	}

	UFunction* FoundTick = nullptr;
	while (Class && !Class->HasAnyClassFlags(CLASS_Native))
	{
		FoundTick = Class->FindFunctionByName(TEXT("ReceiveTick"), EIncludeSuperFlag::ExcludeSuper);
		if (FoundTick)
		{
			break;
		}

		Class = Class->GetSuperClass();
	}
	
	bool bCanTick = FoundTick != nullptr;
	TickFunction->bCanEverTick = bCanTick;
	TickFunction->bStartWithTickEnabled = bCanTick;
}

void FCSGeneratedClassBuilder::ImplementInterfaces(UClass* ManagedClass, const TArray<FCSTypeReferenceMetaData>& Interfaces)
{
	for (const FCSTypeReferenceMetaData& InterfaceData : Interfaces)
	{
		UClass* InterfaceClass = InterfaceData.GetOwningInterface();

		if (!IsValid(InterfaceClass))
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Can't find interface: %s"), *InterfaceData.FieldName.GetName());
			continue;
		}

		FImplementedInterface ImplementedInterface;
		ImplementedInterface.Class = InterfaceClass;
		ImplementedInterface.bImplementedByK2 = true;
		ImplementedInterface.PointerOffset = 0;
		
		ManagedClass->Interfaces.Add(ImplementedInterface);
	}
}

void FCSGeneratedClassBuilder::TryRegisterSubsystem(UClass* ManagedClass)
{
	if (ManagedClass->IsChildOf<UEngineSubsystem>()
	#if WITH_EDITOR
	|| ManagedClass->IsChildOf<UEditorSubsystem>()
	#endif
	)
	{
		FSubsystemCollectionBase::ActivateExternalSubsystem(ManagedClass);
	}
}

void FCSGeneratedClassBuilder::SetConfigName(UClass* ManagedClass, const TSharedPtr<const FCSClassMetaData>& TypeMetaData)
{
	if (TypeMetaData->ClassConfigName.IsNone())
	{
		ManagedClass->ClassConfigName = ManagedClass->GetSuperClass()->ClassConfigName;
	}
	else
	{
		ManagedClass->ClassConfigName = TypeMetaData->ClassConfigName;
	}
}
