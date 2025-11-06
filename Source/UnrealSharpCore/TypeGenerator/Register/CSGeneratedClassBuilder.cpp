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
#include "UnrealSharpCore/TypeGenerator/CSClass.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"
#include "Utils/CSClassUtilities.h"

UCSGeneratedClassBuilder::UCSGeneratedClassBuilder()
{
	RedirectClasses.Add(UDeveloperSettings::StaticClass(), UCSDeveloperSettings::StaticClass());
	FieldType = UCSClass::StaticClass();
}

void UCSGeneratedClassBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UCSClass* Field = static_cast<UCSClass*>(TypeToBuild);
	TSharedPtr<FCSClassMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSClassMetaData>();
	
	UClass* CurrentSuperClass = TryRedirectSuperClass(TypeMetaData, Field->GetSuperClass());
	Field->SetSuperStruct(CurrentSuperClass);

	// Reset for each rebuild of the class, so it doesn't accumulate properties from previous builds.
	Field->NumReplicatedProperties = 0;
	
#if WITH_EDITOR
	CreateOwningBlueprint(TypeMetaData, Field, CurrentSuperClass);

	if (FCSUnrealSharpUtils::IsStandalonePIE())
	{
		CreateClass(TypeMetaData, Field, CurrentSuperClass);
	}
	
	TryUnregisterDynamicSubsystem(Field);
	UCSManager::Get().OnNewClassEvent().Broadcast(Field);
#else
	CreateClass(TypeMetaData, Field, CurrentSuperClass);
#endif
}

#if WITH_EDITOR
void UCSGeneratedClassBuilder::CreateOwningBlueprint(TSharedPtr<FCSClassMetaData> TypeMetaData, UCSClass* Field, UClass* SuperClass)
{
	UBlueprint* Blueprint = Field->GetOwningBlueprint();
	if (IsValid(Blueprint))
	{
		return;
	}

	FString BlueprintName = FCSMetaDataUtils::GetAdjustedFieldName(TypeMetaData->FieldName);
	UPackage* Package = TypeMetaData->GetAsPackage();
	
	Blueprint = NewObject<UCSBlueprint>(Package, *BlueprintName, RF_Public | RF_LoadCompleted | RF_Transient);
	Blueprint->GeneratedClass = Field;
	Blueprint->ParentClass = SuperClass;
	Blueprint->Status = BS_UpToDate;
	Blueprint->BlueprintType = BPTYPE_Normal;
	Blueprint->bLegacyNeedToPurgeSkelRefs = false;
	Blueprint->bIsRegeneratingOnLoad = false;
	Blueprint->bRecompileOnLoad = false;

	if (FCSUnrealSharpUtils::IsStandalonePIE())
	{
		// Some systems still use the skeleton class in standalone,
		// fallback to the main class since we don't have a separate skeleton class in standalone.
		Blueprint->SkeletonGeneratedClass = Field;
	}
		
	Field->SetOwningBlueprint(Blueprint);
}
#endif

void UCSGeneratedClassBuilder::CreateClass(TSharedPtr<FCSClassMetaData> TypeMetaData, UCSClass* Field, UClass* SuperClass)
{
	Field->ClassFlags |= TypeMetaData->ClassFlags;
	Field->ClassFlags |= SuperClass->ClassFlags & CLASS_ScriptInherit;

	Field->PropertyLink = SuperClass->PropertyLink;
	Field->ClassWithin = SuperClass->ClassWithin;
	Field->ClassCastFlags = SuperClass->ClassCastFlags;

	SetConfigName(Field, TypeMetaData);
	
	ImplementInterfaces(Field, TypeMetaData->Interfaces);
	
	FCSPropertyFactory::CreateAndAssignProperties(Field, TypeMetaData->Properties);

	// Build the construction script that will spawn the components
	FCSSimpleConstructionScriptBuilder::BuildSimpleConstructionScript(Field, &Field->SimpleConstructionScript, TypeMetaData->Properties);

#if WITH_EDITOR
	UBlueprint* Blueprint = Field->GetOwningBlueprint();
	Blueprint->SimpleConstructionScript = Field->SimpleConstructionScript;
#endif

	// Generate functions for this class
	FCSFunctionFactory::GenerateVirtualFunctions(Field, TypeMetaData);
	FCSFunctionFactory::GenerateFunctions(Field, TypeMetaData->Functions);

	//Finalize class
	Field->ClassConstructor = &UCSGeneratedClassBuilder::ManagedObjectConstructor;

	Field->Bind();
	Field->StaticLink(true);
	Field->AssembleReferenceTokenStream();

	//Create the default object for this class
	UObject* DefaultObject = Field->GetDefaultObject();
	SetupDefaultTickSettings(DefaultObject, Field);

	Field->SetUpRuntimeReplicationData();
	Field->UpdateCustomPropertyListForPostConstruction();

	RegisterFieldToLoader(Field, ENotifyRegistrationType::NRT_Class);
	TryRegisterDynamicSubsystem(Field);
}

UClass* UCSGeneratedClassBuilder::TryRedirectSuperClass(TSharedPtr<FCSClassMetaData> TypeMetaData, UClass* CurrentSuperClass) const
{
	if (!IsValid(CurrentSuperClass) || CurrentSuperClass->GetFName() != TypeMetaData->ParentClass.FieldName.GetName())
	{
		UClass* SuperClass = TypeMetaData->ParentClass.GetAsClass();
		if (const TWeakObjectPtr<UClass>* RedirectedClass = RedirectClasses.Find(SuperClass))
		{
			SuperClass = RedirectedClass->Get();
		}

		CurrentSuperClass = SuperClass;
	}

	return CurrentSuperClass;
}

FString UCSGeneratedClassBuilder::GetFieldName(TSharedPtr<const FCSTypeReferenceMetaData>& MetaData) const
{
	// Blueprint classes have a _C suffix
	FString FieldName = Super::GetFieldName(MetaData);
	FieldName += TEXT("_C");
	return FieldName;
}

void UCSGeneratedClassBuilder::ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer)
{
	UCSClass* FirstManagedClass = FCSClassUtilities::GetFirstManagedClass(ObjectInitializer.GetClass());
	UClass* FirstNativeClass = FCSClassUtilities::GetFirstNativeClass(FirstManagedClass);
	
	// Execute the native class' constructor first.
	FirstNativeClass->ClassConstructor(ObjectInitializer);

	// Initialize managed properties that are not zero initialized such as FText.
	for (TFieldIterator<FProperty> PropertyIt(FirstManagedClass); PropertyIt; ++PropertyIt)
	{
		FProperty* Property = *PropertyIt;

		if (!FCSClassUtilities::IsManagedClass(Property->GetOwnerClass()))
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

#if WITH_EDITOR
	if (!FirstManagedClass->SetCanBeInstancedFrom())
	{
		return;
	}
#endif
	
	UCSAssembly* Assembly = FirstManagedClass->GetManagedTypeInfo()->GetOwningAssembly();
	Assembly->CreateManagedObject(ObjectInitializer.GetObj());
}

void UCSGeneratedClassBuilder::SetupDefaultTickSettings(UObject* DefaultObject, const UClass* Class)
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
	TickFunction->bStartWithTickEnabled = ParentTickFunction->bStartWithTickEnabled;
	
	if (TickFunction->bCanEverTick && TickFunction->bStartWithTickEnabled)
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

void UCSGeneratedClassBuilder::ImplementInterfaces(UClass* ManagedClass, const TArray<FCSTypeReferenceMetaData>& Interfaces)
{
	for (const FCSTypeReferenceMetaData& InterfaceData : Interfaces)
	{
		UClass* InterfaceClass = InterfaceData.GetAsInterface();

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

void UCSGeneratedClassBuilder::TryRegisterDynamicSubsystem(UClass* ManagedClass)
{
	if (!ManagedClass->IsChildOf<UDynamicSubsystem>())
	{
		return;
	}

	UCSManager::Get().AddDynamicSubsystemClass(ManagedClass);
}

void UCSGeneratedClassBuilder::TryUnregisterDynamicSubsystem(UClass* ManagedClass)
{
	if (!ManagedClass->IsChildOf<UDynamicSubsystem>())
	{
		return;
	}

	// Remove lingering subsystems from hot reload.
	FSubsystemCollectionBase::DeactivateExternalSubsystem(ManagedClass);
}

void UCSGeneratedClassBuilder::SetConfigName(UClass* ManagedClass, const TSharedPtr<const FCSClassMetaData>& TypeMetaData)
{
	if (TypeMetaData->ConfigName.IsNone())
	{
		ManagedClass->ClassConfigName = ManagedClass->GetSuperClass()->ClassConfigName;
	}
	else
	{
		ManagedClass->ClassConfigName = TypeMetaData->ConfigName;
	}
}
