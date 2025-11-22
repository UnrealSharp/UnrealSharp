#include "CSCompilerContext.h"
#include "BlueprintActionDatabase.h"
#include "CSManager.h"
#include "ISettingsModule.h"
#include "BehaviorTree/Tasks/BTTask_BlueprintBase.h"
#include "Blueprint/StateTreeTaskBlueprintBase.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "Types/CSBlueprint.h"
#include "Types/CSClass.h"
#include "Types/CSSkeletonClass.h"
#include "Factories/CSFunctionFactory.h"
#include "Factories/CSPropertyFactory.h"
#include "Utilities/CSMetaDataUtils.h"
#include "CSUnrealSharpEditorSettings.h"
#include "UnrealSharpUtils.h"
#include "Compilers/CSManagedClassCompiler.h"
#include "Compilers/CSSimpleConstructionScriptCompiler.h"
#include "ReflectionData/CSClassReflectionData.h"
#include "Utilities/CSClassUtilities.h"

FCSCompilerContext::FCSCompilerContext(UCSBlueprint* Blueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompilerOptions) : FKismetCompilerContext(Blueprint, InMessageLog, InCompilerOptions)
{
	
}

void FCSCompilerContext::FinishCompilingClass(UClass* Class)
{
	UCSClass* CSClass = static_cast<UCSClass*>(Class);
	CSClass->SetCanBeInstancedFrom(FCSUnrealSharpUtils::IsEngineStartingUp());
	
	Class->ClassConstructor = &UCSClass::ManagedObjectConstructor;
	
	Super::FinishCompilingClass(Class);

	UCSManagedClassCompiler::SetupDefaultTickSettings(NewClass->GetDefaultObject(), NewClass);

	TSharedPtr<FCSClassReflectionData> ClassReflectionData = GetClassInfo()->GetReflectionData<FCSClassReflectionData>();

	// Super call overrides the class flags, so we need to set after that
	Class->ClassFlags |= ClassReflectionData->ClassFlags;

	UCSManagedClassCompiler::SetConfigName(Class, ClassReflectionData);
	TryInitializeAsDeveloperSettings(Class);
	TryFakeNativeClass(Class);
	
	ApplyMetaData();
}

void FCSCompilerContext::OnPostCDOCompiled(const UObject::FPostCDOCompiledContext& Context)
{
	FKismetCompilerContext::OnPostCDOCompiled(Context);

	UCSClass* MainClass = GetMainClass();
	if (MainClass == NewClass)
	{
		UCSManagedClassCompiler::TryRegisterDynamicSubsystem(NewClass);

		if (GEditor)
		{
			FBlueprintActionDatabase::Get().RefreshClassActions(NewClass);
		}
	}
}

void FCSCompilerContext::CreateClassVariablesFromBlueprint()
{
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = GetMainClass()->GetManagedTypeDefinition();
	TSharedPtr<FCSClassReflectionData> ClassReflectionData = ManagedTypeDefinition->GetReflectionData<FCSClassReflectionData>();
	const TArray<FCSPropertyReflectionData>& PropertiesReflectionData = ClassReflectionData->Properties;

	NewClass->PropertyGuids.Empty(PropertiesReflectionData.Num());
	TryValidateSimpleConstructionScript();

	FCSPropertyFactory::CreateAndAssignProperties(NewClass, PropertiesReflectionData, [this](const FProperty* NewProperty)
	{
		FName PropertyName = NewProperty->GetFName();
		FGuid PropertyGuid = FCSUnrealSharpUtils::ConstructGUIDFromName(PropertyName);
		NewClass->PropertyGuids.Add(PropertyName, PropertyGuid);
	});

	// Create dummy variables for the blueprint.
	// They should not get compiled, just there for metadata for different Unreal modules.
	CreateDummyBlueprintVariables(PropertiesReflectionData);
}

void FCSCompilerContext::CleanAndSanitizeClass(UBlueprintGeneratedClass* ClassToClean, UObject*& InOldCDO)
{
	FKismetCompilerContext::CleanAndSanitizeClass(ClassToClean, InOldCDO);
	NewClass->FieldNotifies.Reset();

	TryDeinitializeAsDeveloperSettings(InOldCDO);

	// Too late to generate functions in CreateFunctionList for child blueprints
	GenerateFunctions();
}

void FCSCompilerContext::SpawnNewClass(const FString& NewClassName)
{
	UCSClass* MainClass = GetMainClass();
	
	UCSSkeletonClass* NewSkeletonClass = NewObject<UCSSkeletonClass>(Blueprint->GetOutermost(), *NewClassName, RF_Public | RF_Transactional);
	NewSkeletonClass->SetOwningBlueprint(Blueprint);
	NewSkeletonClass->SetGeneratedClass(MainClass);

	ICSManagedTypeInterface* ManagedType = FCSClassUtilities::GetManagedType(NewSkeletonClass);
	ManagedType->SetManagedTypeDefinition(MainClass->GetManagedTypeDefinition());

	Blueprint->SkeletonGeneratedClass = NewSkeletonClass;
	NewClass = NewSkeletonClass;

	// Skeleton class doesn't generate functions on the first pass.
	// It's done in CleanAndSanitizeClass which doesn't run when the skeleton class is created
	GenerateFunctions();
}

void FCSCompilerContext::AddInterfacesFromBlueprint(UClass* Class)
{
	UCSManagedClassCompiler::ImplementInterfaces(Class, GetReflectionData()->Interfaces);
}

void FCSCompilerContext::CopyTermDefaultsToDefaultObject(UObject* DefaultObject)
{
	UCSManager::Get().FindManagedObject(DefaultObject);
	UCSManagedClassCompiler::SetupDefaultTickSettings(NewClass->GetDefaultObject(), NewClass);
}

void FCSCompilerContext::TryValidateSimpleConstructionScript() const
{
	const TArray<FCSPropertyReflectionData>& Properties = GetReflectionData()->Properties;
	FCSSimpleConstructionScriptCompiler::BuildSimpleConstructionScript(Blueprint->GeneratedClass, &Blueprint->SimpleConstructionScript, Properties);

	if (!Blueprint->SimpleConstructionScript)
	{
		return;
	}

	TArray<USCS_Node*> Nodes;
	for (const FCSPropertyReflectionData& Property : Properties)
	{
		if (Property.InnerType->PropertyType != ECSPropertyType::DefaultComponent)
		{
			continue;
		}

		USimpleConstructionScript* SCS = Blueprint->SimpleConstructionScript;
		USCS_Node* Node = SCS->FindSCSNode(Property.GetName());
		Nodes.Add(Node);
	}

	// Remove all nodes that are not part of the class anymore.
	int32 NumNodes = Blueprint->SimpleConstructionScript->GetAllNodes().Num();
	TArray<USCS_Node*> AllNodes = Blueprint->SimpleConstructionScript->GetAllNodes();
	for (int32 i = NumNodes - 1; i >= 0; --i)
	{
		USCS_Node* Node = AllNodes[i];
		if (!Nodes.Contains(Node))
		{
			Blueprint->SimpleConstructionScript->RemoveNode(Node);
		}
	}

	Blueprint->SimpleConstructionScript->ValidateNodeTemplates(MessageLog);
	Blueprint->SimpleConstructionScript->ValidateNodeVariableNames(MessageLog);
}

void FCSCompilerContext::GenerateFunctions() const
{
	TSharedPtr<const FCSClassReflectionData> ReflectionData = GetReflectionData();

	if (ReflectionData->Overrides.IsEmpty() && ReflectionData->Functions.IsEmpty())
	{
		return;
	}

	FCSFunctionFactory::GenerateVirtualFunctions(NewClass, ReflectionData);
	FCSFunctionFactory::GenerateFunctions(NewClass, ReflectionData->Functions);
}

UCSClass* FCSCompilerContext::GetMainClass() const
{
	return CastChecked<UCSClass>(Blueprint->GeneratedClass);
}

TSharedPtr<const FCSManagedTypeDefinition> FCSCompilerContext::GetClassInfo() const
{
	return GetMainClass()->GetManagedTypeDefinition();
}

TSharedPtr<const FCSClassReflectionData> FCSCompilerContext::GetReflectionData() const
{
	return GetClassInfo()->GetReflectionData<FCSClassReflectionData>();
}

void FCSCompilerContext::TryInitializeAsDeveloperSettings(const UClass* Class) const
{
	if (!FCSClassUtilities::IsDeveloperSettingsClass(Blueprint, Class))
	{
		return;
	}

	UDeveloperSettings* Settings = static_cast<UDeveloperSettings*>(Class->GetDefaultObject());
	ISettingsModule& SettingsModule = FModuleManager::GetModuleChecked<ISettingsModule>("Settings");

	SettingsModule.RegisterSettings(Settings->GetContainerName(), Settings->GetCategoryName(),
	                                Settings->GetSectionName(),
	                                Settings->GetSectionText(),
	                                Settings->GetSectionDescription(),
	                                Settings);

	Settings->LoadConfig();
}

void FCSCompilerContext::TryDeinitializeAsDeveloperSettings(UObject* Settings) const
{
	if (!IsValid(Settings) || !FCSClassUtilities::IsDeveloperSettingsClass(Blueprint, NewClass))
	{
		return;
	}

	ISettingsModule& SettingsModule = FModuleManager::GetModuleChecked<ISettingsModule>("Settings");
	UDeveloperSettings* DeveloperSettings = static_cast<UDeveloperSettings*>(Settings);
	SettingsModule.UnregisterSettings(DeveloperSettings->GetContainerName(), DeveloperSettings->GetCategoryName(),
	                                  DeveloperSettings->GetSectionName());
}

void FCSCompilerContext::TryFakeNativeClass(UClass* Class)
{
	if (!NeedsToFakeNativeClass(Class))
	{
		return;
	}

	// There are systems in Unreal (BehaviorTree, StateTree) which uses the AssetRegistry to find BP classes, since our C# classes are not assets,
	// we need to fake that they're native classes in editor in order to be able to find them. 

	// The functions that are used to find classes are:
	// FGraphNodeClassHelper::BuildClassGraph()
	// FStateTreeNodeClassCache::CacheClasses()
	Class->ClassFlags |= CLASS_Native;
}

void FCSCompilerContext::ApplyMetaData() const
{
	static FString DisplayNameKey = TEXT("DisplayName");
	if (!NewClass->HasMetaData(*DisplayNameKey))
	{
		NewClass->SetMetaData(*DisplayNameKey, *Blueprint->GetName());
	}

	if (GetDefault<UCSUnrealSharpEditorSettings>()->bSuffixGeneratedTypes)
	{
		FString DisplayName = NewClass->GetMetaData(*DisplayNameKey);
		DisplayName += TEXT(" (C#)");
		NewClass->SetMetaData(*DisplayNameKey, *DisplayName);
	}

	FCSMetaDataUtils::ApplyMetaData(GetReflectionData()->MetaData, NewClass);
}

bool FCSCompilerContext::NeedsToFakeNativeClass(UClass* Class)
{
	static TArray ParentClasses =
	{
		UBTTask_BlueprintBase::StaticClass(),
		UStateTreeTaskBlueprintBase::StaticClass(),
	};

	for (UClass* ParentClass : ParentClasses)
	{
		if (Class->IsChildOf(ParentClass))
		{
			return true;
		}
	}

	return false;
}

void FCSCompilerContext::CreateDummyBlueprintVariables(const TArray<FCSPropertyReflectionData>& Properties) const
{
	Blueprint->NewVariables.Empty(Properties.Num());

	for (const FCSPropertyReflectionData& PropertyReflectionData : Properties)
	{
		FBPVariableDescription VariableDescription;
		VariableDescription.FriendlyName = PropertyReflectionData.GetName().ToString();
		VariableDescription.VarName = PropertyReflectionData.GetName();

		for (const FCSMetaDataEntry& MetaData : PropertyReflectionData.MetaData)
		{
			VariableDescription.SetMetaData(*MetaData.Key, MetaData.Value);
		}

		Blueprint->NewVariables.Add(VariableDescription);
	}
}

#undef LOCTEXT_NAMESPACE
