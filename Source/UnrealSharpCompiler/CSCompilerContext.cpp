#include "CSCompilerContext.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSSkeletonClass.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Factories/PropertyGenerators/CSPropertyGenerator.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"

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
}

void FCSCompilerContext::CreateClassVariablesFromBlueprint()
{
	TSharedRef<const FCSharpClassInfo> ClassInfo = GetMainClass()->GetClassInfo();
	const TArray<FCSPropertyMetaData>& Properties = ClassInfo->TypeMetaData->Properties;

	NewClass->PropertyGuids.Empty(Properties.Num());
	TryValidateSimpleConstructionScript(ClassInfo);
	
	for (const FCSPropertyMetaData& Property : Properties)
	{
		FCSPropertyFactory::CreateAndAssignProperty(NewClass, Property);
		NewClass->PropertyGuids.Add(Property.Name, UCSPropertyGenerator::ConstructGUIDFromName(Property.Name));
	}
}

void FCSCompilerContext::CleanAndSanitizeClass(UBlueprintGeneratedClass* ClassToClean, UObject*& InOldCDO)
{
	FKismetCompilerContext::CleanAndSanitizeClass(ClassToClean, InOldCDO);

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
	if (!Blueprint->SimpleConstructionScript)
	{
		return;
	}
	
	TArray<USCS_Node*> Nodes;
	for (const FCSPropertyMetaData& Property : ClassInfo->TypeMetaData->Properties)
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
			Blueprint->SimpleConstructionScript->RemoveNode(Node, true);
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

TSharedRef<const FCSharpClassInfo> FCSCompilerContext::GetClassInfo() const
{
	return GetMainClass()->GetClassInfo();
}

#undef LOCTEXT_NAMESPACE
