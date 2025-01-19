#include "CSBlueprintCompiler.h"

#include "Kismet2/BlueprintEditorUtils.h"
#include "Kismet2/KismetReinstanceUtilities.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"
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
	if (Class->GetName().StartsWith("SKEL_"))
	{
		FKismetCompilerContext::FinishCompilingClass(Class);
		return;
	}
	
	Class->ClassConstructor = &FCSGeneratedClassBuilder::ManagedActorConstructor;
	FKismetCompilerContext::FinishCompilingClass(Class);
}

void FCSCompilerContext::EnsureProperGeneratedClass(UClass*& TargetUClass)
{
	if (TargetUClass && !((UObject*)TargetUClass)->IsA(UCSClass::StaticClass()))
	{
		FKismetCompilerUtilities::ConsignToOblivion(TargetUClass, Blueprint->bIsRegeneratingOnLoad);
		TargetUClass = nullptr;
	}
}

void FCSCompilerContext::CreateFunctionList()
{
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
		FCSFunctionFactory::FinalizeFunctionSetup(Blueprint->SkeletonGeneratedClass, FunctionBase);
	}
}

void FCSCompilerContext::SpawnNewClass(const FString& NewClassName)
{
	UCSClass* SkeletonClass = FindObject<UCSClass>(Blueprint->GetOutermost(), *NewClassName);

	if (SkeletonClass == nullptr)
	{
		SkeletonClass = NewObject<UCSClass>(Blueprint->GetOutermost(), FName(*NewClassName), RF_Public | RF_Transactional);
	}
	
	NewClass = SkeletonClass;
}

#undef LOCTEXT_NAMESPACE