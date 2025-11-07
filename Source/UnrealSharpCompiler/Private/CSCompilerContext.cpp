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
#include "Builders/CSGeneratedClassBuilder.h"
#include "Utilities/CSMetaDataUtils.h"
#include "Builders/CSSimpleConstructionScriptBuilder.h"
#include "CSUnrealSharpEditorSettings.h"
#include "UnrealSharpUtils.h"
#include "Utilities/CSClassUtilities.h"

FCSCompilerContext::FCSCompilerContext(UCSBlueprint* Blueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompilerOptions) : FKismetCompilerContext(Blueprint, InMessageLog, InCompilerOptions)
{
	
}

void FCSCompilerContext::FinishCompilingClass(UClass* Class)
{
	UCSClass* CSClass = static_cast<UCSClass*>(Class);
	CSClass->SetCanBeInstancedFrom(FCSUnrealSharpUtils::IsEngineStartingUp());
	
	Class->ClassConstructor = &UCSGeneratedClassBuilder::ManagedObjectConstructor;
	
	Super::FinishCompilingClass(Class);

	UCSGeneratedClassBuilder::SetupDefaultTickSettings(NewClass->GetDefaultObject(), NewClass);

	TSharedPtr<FCSClassMetaData> TypeMetaData = GetClassInfo()->GetMetaData<FCSClassMetaData>();

	// Super call overrides the class flags, so we need to set after that
	Class->ClassFlags |= TypeMetaData->ClassFlags;

	UCSGeneratedClassBuilder::SetConfigName(Class, TypeMetaData);
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
		UCSGeneratedClassBuilder::TryRegisterDynamicSubsystem(NewClass);

		if (GEditor)
		{
			FBlueprintActionDatabase::Get().RefreshClassActions(NewClass);
		}
	}
}

void FCSCompilerContext::CreateClassVariablesFromBlueprint()
{
	TSharedPtr<FCSManagedTypeInfo> TypeInfo = GetMainClass()->GetManagedTypeInfo();
	const TArray<FCSPropertyMetaData>& Properties = TypeInfo->GetMetaData<FCSClassMetaData>()->Properties;

	NewClass->PropertyGuids.Empty(Properties.Num());
	TryValidateSimpleConstructionScript();

	FCSPropertyFactory::CreateAndAssignProperties(NewClass, Properties, [this](const FProperty* NewProperty)
	{
		FName PropertyName = NewProperty->GetFName();
		FGuid PropertyGuid = FCSUnrealSharpUtils::ConstructGUIDFromName(PropertyName);
		NewClass->PropertyGuids.Add(PropertyName, PropertyGuid);
	});

	// Create dummy variables for the blueprint.
	// They should not get compiled, just there for metadata for different Unreal modules.
	CreateDummyBlueprintVariables(Properties);
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
	ManagedType->SetTypeInfo(MainClass->GetManagedTypeInfo());

	Blueprint->SkeletonGeneratedClass = NewSkeletonClass;
	NewClass = NewSkeletonClass;

	// Skeleton class doesn't generate functions on the first pass.
	// It's done in CleanAndSanitizeClass which doesn't run when the skeleton class is created
	GenerateFunctions();
}

void FCSCompilerContext::AddInterfacesFromBlueprint(UClass* Class)
{
	UCSGeneratedClassBuilder::ImplementInterfaces(Class, GetTypeMetaData()->Interfaces);
}

void FCSCompilerContext::CopyTermDefaultsToDefaultObject(UObject* DefaultObject)
{
	UCSManager::Get().FindManagedObject(DefaultObject);
	UCSGeneratedClassBuilder::SetupDefaultTickSettings(NewClass->GetDefaultObject(), NewClass);
}

void FCSCompilerContext::TryValidateSimpleConstructionScript() const
{
	const TArray<FCSPropertyMetaData>& Properties = GetTypeMetaData()->Properties;
	FCSSimpleConstructionScriptBuilder::BuildSimpleConstructionScript(Blueprint->GeneratedClass, &Blueprint->SimpleConstructionScript, Properties);

	if (!Blueprint->SimpleConstructionScript)
	{
		return;
	}

	TArray<USCS_Node*> Nodes;
	for (const FCSPropertyMetaData& Property : Properties)
	{
		if (Property.Type->PropertyType != ECSPropertyType::DefaultComponent)
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
	TSharedPtr<const FCSClassMetaData> TypeMetaData = GetTypeMetaData();

	if (TypeMetaData->VirtualFunctions.IsEmpty() && TypeMetaData->Functions.IsEmpty())
	{
		return;
	}

	FCSFunctionFactory::GenerateVirtualFunctions(NewClass, TypeMetaData);
	FCSFunctionFactory::GenerateFunctions(NewClass, TypeMetaData->Functions);
}

UCSClass* FCSCompilerContext::GetMainClass() const
{
	return CastChecked<UCSClass>(Blueprint->GeneratedClass);
}

TSharedPtr<const FCSManagedTypeInfo> FCSCompilerContext::GetClassInfo() const
{
	return GetMainClass()->GetManagedTypeInfo();
}

TSharedPtr<const FCSClassMetaData> FCSCompilerContext::GetTypeMetaData() const
{
	return GetClassInfo()->GetMetaData<FCSClassMetaData>();
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

void FCSCompilerContext::ApplyMetaData()
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

	FCSMetaDataUtils::ApplyMetaData(GetTypeMetaData()->MetaData, NewClass);
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

void FCSCompilerContext::CreateDummyBlueprintVariables(const TArray<FCSPropertyMetaData>& Properties) const
{
	Blueprint->NewVariables.Empty(Properties.Num());

	for (const FCSPropertyMetaData& PropertyMetaData : Properties)
	{
		FBPVariableDescription VariableDescription;
		VariableDescription.FriendlyName = PropertyMetaData.GetName().ToString();
		VariableDescription.VarName = PropertyMetaData.GetName();

		for (const TTuple<FString, FString>& MetaData : PropertyMetaData.MetaData)
		{
			VariableDescription.SetMetaData(*MetaData.Key, MetaData.Value);
		}

		Blueprint->NewVariables.Add(VariableDescription);
	}
}

#undef LOCTEXT_NAMESPACE
