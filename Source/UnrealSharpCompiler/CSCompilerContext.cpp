#include "CSCompilerContext.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
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
		Class->ClassConstructor = &FCSGeneratedClassBuilder::ManagedActorConstructor;
	}

	Super::FinishCompilingClass(Class);
}

void FCSCompilerContext::CreateFunctionList()
{
	// Don't need the super. Only the delegate part which I've copied over down below
	//Super::CreateFunctionList();
	
	for (int32 i = 0; i < Blueprint->DelegateSignatureGraphs.Num(); ++i)
	{
		ProcessOneFunctionGraph(Blueprint->DelegateSignatureGraphs[i]);
	}
}

void FCSCompilerContext::CreateClassVariablesFromBlueprint()
{
	Super::CreateClassVariablesFromBlueprint();
	
	UCSClass* MainClass = GetMainClass();
	TSharedRef<const FCSharpClassInfo> ClassInfo = MainClass->GetClassInfo();
	const TArray<FCSPropertyMetaData>& Properties = ClassInfo->TypeMetaData->Properties;
	
	for (const FCSPropertyMetaData& Property : Properties)
	{
		if (Property.Type->PropertyType != ECSPropertyType::Delegate)
		{
			continue;
		}
		
		FCSPropertyFactory::CreateProperty(MainClass, Property);
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
	NewClass = NewObject<UBlueprintGeneratedClass>(Blueprint->GetOutermost(), FName(*NewClassName), RF_Public | RF_Transactional);
	
	// Skeleton class doesn't generate functions on the first pass.
	// It's done in CleanAndSanitizeClass which doesn't run when the skeleton class is created
	GenerateFunctions();
}

void FCSCompilerContext::GenerateFunctions() const
{
	UCSClass* MainClass = GetMainClass();
	TSharedPtr<FCSClassMetaData> TypeMetaData = MainClass->GetClassInfo()->TypeMetaData;

	if (TypeMetaData->VirtualFunctions.IsEmpty() && TypeMetaData->Functions.IsEmpty())
	{
		return;
	}

	TArray<UCSFunctionBase*> FunctionBases;
	FCSFunctionFactory::GenerateVirtualFunctions(NewClass, TypeMetaData, FunctionBases);
	FCSFunctionFactory::GenerateFunctions(NewClass, TypeMetaData->Functions, FunctionBases);
}

UCSClass* FCSCompilerContext::GetMainClass() const
{
	return CastChecked<UCSClass>(Blueprint->GeneratedClass);
}

#undef LOCTEXT_NAMESPACE