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
#include "UnrealSharpCore/TypeGenerator/CSClass.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

FCSGeneratedClassBuilder::FCSGeneratedClassBuilder(const TSharedPtr<FCSClassMetaData>& InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly): TCSGeneratedTypeBuilder(InTypeMetaData, InOwningAssembly)
{
	RedirectClasses.Add(UDeveloperSettings::StaticClass(), UCSDeveloperSettings::StaticClass());
}

void FCSGeneratedClassBuilder::RebuildType()
{
	if (!Field->GetClassInfo().IsValid())
	{
		TSharedPtr<FCSharpClassInfo> ClassInfo = OwningAssembly->FindClassInfo(TypeMetaData->FieldName);
		Field->SetClassInfo(ClassInfo);
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
	UpdateClassDefaultObject();
	UCSManager::Get().OnClassReloadedEvent().Broadcast(Field);
}

void FCSGeneratedClassBuilder::CreateClassEditor(UClass* SuperClass)
{
	CreateBlueprint(SuperClass);
	UCSManager::Get().OnNewClassEvent().Broadcast(Field);
}

void FCSGeneratedClassBuilder::UpdateClassDefaultObject() const
{
	UObject* ClassDefaultObject = Field->ClassDefaultObject;
	ClassDefaultObject->ClearFlags(RF_Public);
	ClassDefaultObject->SetFlags(RF_Transient);
	ClassDefaultObject->RemoveFromRoot();
	ClassDefaultObject->Rename(nullptr, GetTransientPackage(), REN_ForceNoResetLoaders | REN_DontCreateRedirectors | REN_DoNotDirty | REN_NonTransactional);
	ClassDefaultObject->MarkAsGarbage();
	Field->ClassDefaultObject = nullptr;
	
	OwningAssembly->RemoveManagedObject(ClassDefaultObject);
	
	Field->GetDefaultObject(true);
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
		Blueprint->BlueprintNamespace = TypeMetaData->FieldName.GetNamespace().GetName();
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
	UCSClass* ManagedClass = GetFirstManagedClass(ObjectInitializer.GetClass());
	
	//Execute the native class' constructor first.
	UClass* NativeClass = GetFirstNativeClass(ObjectInitializer.GetClass());
	NativeClass->ClassConstructor(ObjectInitializer);

	// Initialize properties that are not zero initialized such as FText.
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

	TSharedPtr<FCSAssembly> OwningAssembly = ManagedClass->GetOwningAssembly();
	OwningAssembly->CreateManagedObject(ObjectInitializer.GetObj());
}

void FCSGeneratedClassBuilder::SetupDefaultTickSettings(UObject* DefaultObject, const UClass* Class)
{
	FTickFunction* TickFunction;
	if (AActor* Actor = Cast<AActor>(DefaultObject))
	{
		TickFunction = &Actor->PrimaryActorTick;
	}
	else if (UActorComponent* ActorComponent = Cast<UActorComponent>(DefaultObject))
	{
		TickFunction = &ActorComponent->PrimaryComponentTick;
	}
	else
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

UCSClass* FCSGeneratedClassBuilder::GetFirstManagedClass(UClass* Class)
{
	if (Class->HasAnyClassFlags(CLASS_Native))
	{
		return nullptr;
	}
	
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
	while (Class->HasAnyClassFlags(CLASS_CompiledFromBlueprint) && !IsManagedType(Class))
	{
		Class = Class->GetSuperClass();
	}
	return Class;
}

bool FCSGeneratedClassBuilder::IsManagedType(const UClass* Class)
{
	return Class->GetClass() == UCSClass::StaticClass();
}

bool FCSGeneratedClassBuilder::IsSkeletonType(const UClass* Class)
{
	return Class->GetClass() == UCSSkeletonClass::StaticClass();
}
