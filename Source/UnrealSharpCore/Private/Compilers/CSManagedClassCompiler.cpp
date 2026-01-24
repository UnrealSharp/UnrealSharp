#include "Compilers/CSManagedClassCompiler.h"
#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "Compilers/CSSimpleConstructionScriptCompiler.h"
#include "Utilities/CSMetaDataUtils.h"
#include "Types/CSBlueprint.h"
#include "UObject/UnrealType.h"
#include "Engine/Blueprint.h"
#include "Extensions/DeveloperSettings/CSDeveloperSettings.h"
#include "Types/CSClass.h"
#include "Factories/CSFunctionFactory.h"
#include "Factories/CSPropertyFactory.h"
#include "UnrealSharpUtils.h"
#include "Utilities/CSClassUtilities.h"

#if WITH_EDITOR
#include "BlueprintActionDatabase.h"
#endif

UCSManagedClassCompiler::UCSManagedClassCompiler()
{
	RedirectClasses.Add(UDeveloperSettings::StaticClass(), UCSDeveloperSettings::StaticClass());
	FieldType = UCSClass::StaticClass();
}

void UCSManagedClassCompiler::Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const
{
	UCSClass* Field = static_cast<UCSClass*>(TypeToRecompile);
	TSharedPtr<FCSClassReflectionData> ClassReflectionData = ManagedTypeDefinition->GetReflectionData<FCSClassReflectionData>();
	
	UClass* NewSuperClass = TryRedirectSuperClass(ClassReflectionData, Field->GetSuperClass());
	UClass* CurrentSuperClass = Field->GetSuperClass();

	if (!IsValid(CurrentSuperClass))
	{
		// Initial setup. BP-compiler will handle future re-parenting.
		Field->SetSuperStruct(NewSuperClass);
	}

#if WITH_EDITOR
	CreateOrUpdateOwningBlueprint(ClassReflectionData, Field, NewSuperClass);

	if (FCSUnrealSharpUtils::IsStandalonePIE())
	{
		// BP-compiler is not running in standalone PIE, so we need to recompile the class ourselves.
		CompileClass(ClassReflectionData, Field, NewSuperClass);
	}
	
	DeactivateSubsystem(Field);
	UCSManager::Get().OnNewClassEvent().Broadcast(Field);
#else
	CompileClass(ClassReflectionData, Field, NewSuperClass);
#endif
}

#if WITH_EDITOR
void UCSManagedClassCompiler::CreateOrUpdateOwningBlueprint(TSharedPtr<FCSClassReflectionData> ClassReflectionData, UCSClass* Field, UClass* SuperClass)
{
	UBlueprint* Blueprint = Field->GetOwningBlueprint();
	
	if (!IsValid(Blueprint))
	{
		FString BlueprintName = FCSMetaDataUtils::GetAdjustedFieldName(ClassReflectionData->FieldName);
		UPackage* Package = ClassReflectionData->GetAsPackage();
	
		Blueprint = NewObject<UCSBlueprint>(Package, *BlueprintName, RF_Public | RF_LoadCompleted);
		Blueprint->GeneratedClass = Field;

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
	
	PopulateComponentOverrides(&Blueprint->ComponentClassOverrides, ClassReflectionData);
	Blueprint->ParentClass = SuperClass;
}
#endif

void UCSManagedClassCompiler::CompileClass(TSharedPtr<FCSClassReflectionData> ClassReflectionData, UCSClass* Field, UClass* SuperClass)
{
	SetClassFlags(Field, ClassReflectionData);
	SetConfigName(Field, ClassReflectionData);
	
	ImplementInterfaces(Field, ClassReflectionData->Interfaces);
	
	FCSPropertyFactory::CreateAndAssignProperties(Field, ClassReflectionData->Properties);

	// Build the construction script that will spawn the components
	FCSSimpleConstructionScriptCompiler::CompileSimpleConstructionScript(Field, &Field->SimpleConstructionScript, ClassReflectionData->Properties);

#if WITH_EDITOR
	UBlueprint* Blueprint = Field->GetOwningBlueprint();
	Blueprint->SimpleConstructionScript = Field->SimpleConstructionScript;
#endif
	
	PopulateComponentOverrides(&Field->ComponentClassOverrides, ClassReflectionData);
	
	FCSFunctionFactory::GenerateVirtualFunctions(Field, ClassReflectionData);
	FCSFunctionFactory::GenerateFunctions(Field, ClassReflectionData->Functions);
	
	Field->ClassConstructor = &UCSClass::ManagedObjectConstructor;

	Field->Bind();
	Field->StaticLink(true);
	Field->AssembleReferenceTokenStream();
	
	CreateDeferredManagedCDO(Field);
	FinalizeManagedCDO(Field);

	Field->SetUpRuntimeReplicationData();
	Field->UpdateCustomPropertyListForPostConstruction();

	RegisterFieldToLoader(Field, ENotifyRegistrationType::NRT_Class);
	ActivateSubsystem(Field);
}

void UCSManagedClassCompiler::PopulateComponentOverrides(TArray<FBPComponentClassOverride>* Overrides, const TSharedPtr<FCSClassReflectionData>& ClassReflectionData)
{
	if (ClassReflectionData->ComponentOverrides.IsEmpty())
	{
		return;
	}
	
	Overrides->Reset(ClassReflectionData->ComponentOverrides.Num());
	
	for (const FCSComponentOverrideReflectionData& OverrideData : ClassReflectionData->ComponentOverrides)
	{
		UClass* ComponentClass = OverrideData.ComponentType.GetAsClass();
		
		if (!IsValid(ComponentClass))
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Can't find component class: %s"), *OverrideData.ComponentType.FieldName.GetName());
			continue;
		}
		
		UClass* ParentClass = OverrideData.OwningClass.GetAsClass();
		
		if (!IsValid(ParentClass) || !FCSClassUtilities::IsNativeClass(ParentClass))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Can't find native owning class {0} for component override", *OverrideData.OwningClass.FieldName.GetName());
			continue;
		}
		
		FObjectProperty* FoundProperty = FindFProperty<FObjectProperty>(ParentClass, OverrideData.PropertyName);
		
		if (!FoundProperty)
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Can't find component property {0} on class {1}", *OverrideData.PropertyName.ToString(), *ParentClass->GetName());
			continue;
		}
		
		AActor* OwnerActor = FoundProperty->GetOwnerClass()->GetDefaultObject<AActor>();
		if (!IsValid(OwnerActor))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Owner of component property {0} is not a valid Actor on default object of class {1}", *OverrideData.PropertyName.ToString(), *ParentClass->GetName());
			continue;
		}
		
		UObject* Component = FoundProperty->GetObjectPropertyValue_InContainer(OwnerActor);
		
		if (!IsValid(Component))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Component property {0} is not initialized on default object of class {1}", *OverrideData.PropertyName.ToString(), *ParentClass->GetName());
			continue;
		}
		
		FBPComponentClassOverride& NewOverride = Overrides->AddDefaulted_GetRef();
		NewOverride.ComponentClass = ComponentClass;
		NewOverride.ComponentName = Component->GetFName();
	}
}

UClass* UCSManagedClassCompiler::TryRedirectSuperClass(TSharedPtr<FCSClassReflectionData> ClassReflectionData, UClass* CurrentSuperClass) const
{
	if (!IsValid(CurrentSuperClass) || CurrentSuperClass->GetFName() != ClassReflectionData->ParentClass.FieldName.GetName())
	{
		UClass* SuperClass = ClassReflectionData->ParentClass.GetAsClass();
		if (const TWeakObjectPtr<UClass>* RedirectedClass = RedirectClasses.Find(SuperClass))
		{
			SuperClass = RedirectedClass->Get();
		}

		CurrentSuperClass = SuperClass;
	}

	return CurrentSuperClass;
}

FString UCSManagedClassCompiler::GetFieldName(TSharedPtr<const FCSTypeReferenceReflectionData>& ReflectionData) const
{
	// Blueprint classes have a _C suffix
	FString FieldName = Super::GetFieldName(ReflectionData);
	FieldName += TEXT("_C");
	return FieldName;
}

TSharedPtr<FCSTypeReferenceReflectionData> UCSManagedClassCompiler::CreateNewReflectionData() const
{
	return MakeShared<FCSClassReflectionData>();
}

void UCSManagedClassCompiler::SetupDefaultTickSettings(UObject* DefaultObject, const UClass* Class)
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

UObject* UCSManagedClassCompiler::CreateDeferredManagedCDO(UCSClass* ManagedClass)
{
	ManagedClass->SetDeferredCreation(true);
	return ManagedClass->GetDefaultObject(true);
}

void UCSManagedClassCompiler::FinalizeManagedCDO(UCSClass* ManagedClass)
{
	UCSManagedAssembly* ManagedAssembly = ManagedClass->GetOwningAssembly();
	UObject* DefaultObject = ManagedClass->GetDefaultObject(false);
	check(IsValid(DefaultObject));
	
	ManagedClass->SetDeferredCreation(false);
	ManagedAssembly->CreateManagedObjectFromNative(DefaultObject, ManagedClass->GetTypeGCHandle());
	SetupDefaultTickSettings(DefaultObject, ManagedClass);
}

void UCSManagedClassCompiler::ImplementInterfaces(UClass* ManagedClass, const TArray<FCSTypeReferenceReflectionData>& Interfaces)
{
	for (const FCSTypeReferenceReflectionData& InterfaceData : Interfaces)
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

void UCSManagedClassCompiler::ActivateSubsystem(TSubclassOf<USubsystem> SubsystemClass)
{
	if (!IsValid(SubsystemClass) || !SubsystemClass->IsChildOf<USubsystem>())
	{
		return;
	}
	
	UCSManager::Get().ActivateSubsystemClass(SubsystemClass);
}

void UCSManagedClassCompiler::DeactivateSubsystem(TSubclassOf<USubsystem> SubsystemClass)
{
	if (!IsValid(SubsystemClass) || !SubsystemClass->IsChildOf<USubsystem>())
	{
		return;
	}
	
	FSubsystemCollectionBase::DeactivateExternalSubsystem(SubsystemClass);
}

#if WITH_EDITOR
void UCSManagedClassCompiler::RefreshClassActions(UClass* ClassToRefresh)
{
	if (!IsValid(GEditor))
	{
		return;
	}
	
	FBlueprintActionDatabase::Get().RefreshClassActions(ClassToRefresh);
}
#endif

void UCSManagedClassCompiler::SetConfigName(UClass* ManagedClass, const TSharedPtr<const FCSClassReflectionData>& ClassReflectionData)
{
	if (ClassReflectionData->Config.IsNone())
	{
		ManagedClass->ClassConfigName = ManagedClass->GetSuperClass()->ClassConfigName;
	}
	else
	{
		ManagedClass->ClassConfigName = ClassReflectionData->Config;
	}
}

void UCSManagedClassCompiler::SetClassFlags(UClass* ManagedClass, const TSharedPtr<const FCSClassReflectionData>& ClassReflectionData)
{
	UClass* SuperClass = ManagedClass->GetSuperClass();
	
	ManagedClass->ClassFlags = ClassReflectionData->ClassFlags;
	ManagedClass->ClassFlags |= SuperClass->ClassFlags & CLASS_ScriptInherit;

	ManagedClass->PropertyLink = SuperClass->PropertyLink;
	ManagedClass->ClassWithin = SuperClass->ClassWithin;
	ManagedClass->ClassCastFlags = SuperClass->ClassCastFlags;
}
