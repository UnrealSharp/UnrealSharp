#include "CSCompilerContext.h"

#include "BlueprintActionDatabase.h"
#include "ISettingsModule.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSSkeletonClass.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Factories/PropertyGenerators/CSPropertyGenerator.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"
#include "TypeGenerator/Register/CSSimpleConstructionScriptBuilder.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"
#include "UnrealSharpEditor/CSUnrealSharpEditorSettings.h"

FCSCompilerContext::FCSCompilerContext(UCSBlueprint* Blueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompilerOptions):
	FKismetCompilerContext(Blueprint, InMessageLog, InCompilerOptions)
{
	
}

void FCSCompilerContext::FinishCompilingClass(UClass* Class)
{
	// The skeleton class shouldn't be using the managed constructor
	if (FCSGeneratedClassBuilder::IsManagedType(Class))
	{
		Class->ClassConstructor = &FCSGeneratedClassBuilder::ManagedObjectConstructor;
	}

	Super::FinishCompilingClass(Class);

	TSharedPtr<FCSClassMetaData> TypeMetaData = GetClassInfo()->TypeMetaData;
	Class->ClassFlags |= TypeMetaData->ClassFlags;
	
	FCSGeneratedClassBuilder::SetConfigName(Class, TypeMetaData);
	TryInitializeAsDeveloperSettings(Class);

	ApplyMetaData();
}

void FCSCompilerContext::OnPostCDOCompiled(const UObject::FPostCDOCompiledContext& Context)
{
	FKismetCompilerContext::OnPostCDOCompiled(Context);
	
	FCSGeneratedClassBuilder::SetupDefaultTickSettings(NewClass->GetDefaultObject(), NewClass);
	
	UCSClass* Class = GetMainClass();
	if (Class == NewClass)
	{
		FCSGeneratedClassBuilder::TryRegisterSubsystem(Class);
		
		if (GEditor)
		{
			FBlueprintActionDatabase::Get().RefreshClassActions(Class);
		}
	}
}

void FCSCompilerContext::CreateClassVariablesFromBlueprint()
{
	TSharedPtr<FCSharpClassInfo> ClassInfo = GetMainClass()->GetClassInfo();
	const TArray<FCSPropertyMetaData>& Properties = ClassInfo->TypeMetaData->Properties;

	NewClass->PropertyGuids.Empty(Properties.Num());
	TryValidateSimpleConstructionScript(ClassInfo);
	
	FCSPropertyFactory::CreateAndAssignProperties(NewClass, Properties, [this](const FProperty* NewProperty)
	{
		FName PropertyName = NewProperty->GetFName();
		FGuid PropertyGuid = UCSPropertyGenerator::ConstructGUIDFromName(PropertyName);
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
	UCSSkeletonClass* NewSkeletonClass = NewObject<UCSSkeletonClass>(Blueprint->GetOutermost(), FName(*NewClassName), RF_Public | RF_Transactional);
	NewSkeletonClass->SetGeneratedClass(MainClass);
	NewClass = NewSkeletonClass;
	
	// Skeleton class doesn't generate functions on the first pass.
	// It's done in CleanAndSanitizeClass which doesn't run when the skeleton class is created
	GenerateFunctions();
}

void FCSCompilerContext::AddInterfacesFromBlueprint(UClass* Class)
{
	TSharedPtr<FCSClassMetaData> TypeMetaData = GetClassInfo()->TypeMetaData;
	FCSGeneratedClassBuilder::ImplementInterfaces(Class, TypeMetaData->Interfaces);
}

void FCSCompilerContext::TryValidateSimpleConstructionScript(const TSharedPtr<const FCSharpClassInfo>& ClassInfo) const
{
	const TArray<FCSPropertyMetaData>& Properties = ClassInfo->TypeMetaData->Properties;
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
		USCS_Node* Node = SCS->FindSCSNode(Property.Name);
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
	UCSClass* MainClass = GetMainClass();
	TSharedPtr<FCSClassMetaData> TypeMetaData = MainClass->GetClassInfo()->TypeMetaData;

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

TSharedPtr<const FCSharpClassInfo> FCSCompilerContext::GetClassInfo() const
{
	return GetMainClass()->GetClassInfo();
}

bool FCSCompilerContext::IsDeveloperSettings() const
{
	return Blueprint->GeneratedClass == NewClass && NewClass->IsChildOf<UDeveloperSettings>();
}

void FCSCompilerContext::TryInitializeAsDeveloperSettings(const UClass* Class) const
{
	if (!IsDeveloperSettings())
	{
		return;
	}

	UDeveloperSettings* Settings = static_cast<UDeveloperSettings*>(Class->GetDefaultObject());
	ISettingsModule& SettingsModule = FModuleManager::GetModuleChecked<ISettingsModule>("Settings");
		
	SettingsModule.RegisterSettings(Settings->GetContainerName(), Settings->GetCategoryName(), Settings->GetSectionName(),
		Settings->GetSectionText(),
		Settings->GetSectionDescription(),
		Settings);

	Settings->LoadConfig();
}

void FCSCompilerContext::TryDeinitializeAsDeveloperSettings(UObject* Settings) const
{
	if (!IsValid(Settings) || !IsDeveloperSettings())
	{
		return;
	}

	ISettingsModule& SettingsModule = FModuleManager::GetModuleChecked<ISettingsModule>("Settings");
	UDeveloperSettings* DeveloperSettings = static_cast<UDeveloperSettings*>(Settings);
	SettingsModule.UnregisterSettings(DeveloperSettings->GetContainerName(), DeveloperSettings->GetCategoryName(), DeveloperSettings->GetSectionName());
}

void FCSCompilerContext::ApplyMetaData()
{
	TSharedPtr<const FCSharpClassInfo> ClassInfo = GetClassInfo();
	TSharedPtr<const FCSClassMetaData> TypeMetaData = ClassInfo->TypeMetaData;
		
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

	FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, NewClass);
}

void FCSCompilerContext::CreateDummyBlueprintVariables(const TArray<FCSPropertyMetaData>& Properties) const
{
	Blueprint->NewVariables.Empty(Properties.Num());

	for (const FCSPropertyMetaData& PropertyMetaData : Properties)
	{
		FBPVariableDescription VariableDescription;
		VariableDescription.FriendlyName = PropertyMetaData.Name.ToString();
		VariableDescription.VarName = PropertyMetaData.Name;

		for (const TTuple<FString, FString>& MetaData : PropertyMetaData.MetaData)
		{
			VariableDescription.SetMetaData(*MetaData.Key, MetaData.Value);
		}

		Blueprint->NewVariables.Add(VariableDescription);
	}
}

#undef LOCTEXT_NAMESPACE
