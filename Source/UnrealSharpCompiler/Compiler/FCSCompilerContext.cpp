#include "FCSCompilerContext.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"

#define LOCTEXT_NAMESPACE "AdventureCompilerContext"

bool FCSBlueprintCompiler::CanCompile(const UBlueprint* Blueprint)
{
	return Cast<UCSBlueprint>(Blueprint) != nullptr;
}

void FCSBlueprintCompiler::Compile(UBlueprint* Blueprint, const FKismetCompilerOptions& CompileOptions, FCompilerResultsLog& Results)
{
	if (UCSBlueprint* CSClass = Cast<UCSBlueprint>(Blueprint))
	{
		FCSCompilerContext Compiler(CSClass, Results, CompileOptions);
		Compiler.Compile();
	}
}

FCSBlueprintCompiler::FCSBlueprintCompiler()
{
}

FCSCompilerContext::FCSCompilerContext(UCSBlueprint* SourceAdventureBP, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompilerOptions) : Super(SourceAdventureBP, InMessageLog, InCompilerOptions)
{
	
}

void FCSCompilerContext::CreateFunctionList()
{
	FKismetCompilerContext::CreateFunctionList();

	UCSClass* ManagedClass = static_cast<UCSClass*>(Blueprint->GeneratedClass.Get());
	UClass* SkeletonClass = Blueprint->SkeletonGeneratedClass;
	
	TSharedRef<FCSharpClassInfo> ClassMetaDataRef = ManagedClass->GetClassInfo();
	TSharedRef<FCSClassMetaData> ClassMetaData = ClassMetaDataRef->TypeMetaData.ToSharedRef();

	TArray<UCSFunctionBase*> Functions;
	FCSFunctionFactory::GenerateVirtualFunctions(ManagedClass, ClassMetaData, &Functions);
	FCSFunctionFactory::GenerateFunctions(ManagedClass, ClassMetaData->Functions, &Functions);

	// Add them to the skeleton class so they show up in the blueprint editor
	for (UCSFunctionBase* Function : Functions)
	{
		FCSFunctionFactory::AddFunctionToOuter(SkeletonClass, Function);
	}
}

#undef LOCTEXT_NAMESPACE


