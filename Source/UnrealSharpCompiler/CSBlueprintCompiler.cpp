#include "CSBlueprintCompiler.h"

#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"

#define LOCTEXT_NAMESPACE "AdventureCompilerContext"

bool FCSBlueprintCompiler::CanCompile(const UBlueprint* Blueprint)
{
	return Cast<UCSBlueprint>(Blueprint) != nullptr;
}

void FCSBlueprintCompiler::Compile(UBlueprint* Blueprint, const FKismetCompilerOptions& CompileOptions, FCompilerResultsLog& Results)
{
	if (UCSBlueprint* CSBlueprint = CastChecked<UCSBlueprint>(Blueprint))
	{
		FCSCompilerContext Compiler(CSBlueprint, Results, CompileOptions);
		Compiler.Compile();
		check(Compiler.NewClass);
	}
}

FCSBlueprintCompiler::FCSBlueprintCompiler()
{
}

FCSCompilerContext::FCSCompilerContext(UCSBlueprint* SourceAdventureBP, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompilerOptions) : Super(SourceAdventureBP, InMessageLog, InCompilerOptions)
{
}

void FCSCompilerContext::FinishCompilingClass(UClass* Class)
{
	Class->ClassConstructor = &FCSGeneratedClassBuilder::ManagedActorConstructor;
	FKismetCompilerContext::FinishCompilingClass(Class);
}

void FCSCompilerContext::CreateFunctionList()
{
	Super::CreateFunctionList();
	for (int32 i = 0; i < Blueprint->DelegateSignatureGraphs.Num(); ++i)
	{
		ProcessOneFunctionGraph(Blueprint->DelegateSignatureGraphs[i]);
	}
	
	TArray<UCSFunctionBase*> FunctionBases;
	UCSClass* MainClass = Cast<UCSClass>(Blueprint->GeneratedClass);
	TSharedPtr<FCSClassMetaData> TypeMetaData = MainClass->GetClassInfo()->TypeMetaData;

	if (TypeMetaData->VirtualFunctions.IsEmpty() && TypeMetaData->Functions.IsEmpty())
	{
		return;
	}
	
	FCSFunctionFactory::GenerateVirtualFunctions(MainClass, TypeMetaData, FunctionBases);
	FCSFunctionFactory::GenerateFunctions(MainClass, TypeMetaData->Functions, FunctionBases);
	
	for (UCSFunctionBase* FunctionBase : FunctionBases)
	{
		// Add this to the skeleton so functions show up in the blueprint editor
		FCSFunctionFactory::FinalizeFunctionSetup(Blueprint->SkeletonGeneratedClass, FunctionBase);
	}
}

void FCSCompilerContext::CreateClassVariablesFromBlueprint()
{
	Super::CreateClassVariablesFromBlueprint();
	
	UCSClass* MainClass = CastChecked<UCSClass>(NewClass);
	const TArray<FCSPropertyMetaData>& Properties = MainClass->GetClassInfo()->TypeMetaData->Properties;
	
	for (const FCSPropertyMetaData& Property : Properties)
	{
		if (Property.Type->PropertyType == ECSPropertyType::Delegate)
		{
			FCSPropertyFactory::CreatePropertyEditor(MainClass, Property);
		}
	}
}

void FCSCompilerContext::SpawnNewClass(const FString& NewClassName)
{
	UCSClass* SkeletonClass = FindObject<UCSClass>(Blueprint->GetOutermost(), *NewClassName);

	if (SkeletonClass == nullptr)
	{
		SkeletonClass = NewObject<UCSClass>(Blueprint->GetOutermost(), FName(*NewClassName), RF_Public | RF_Transactional);

		TSharedPtr<FCSharpClassInfo> ClassInfo = FCSTypeRegistry::Get().GetClassInfoFromName(Blueprint->GetFName());
		SkeletonClass->SetClassMetaData(ClassInfo);
	}
	
	NewClass = SkeletonClass;
}

#undef LOCTEXT_NAMESPACE