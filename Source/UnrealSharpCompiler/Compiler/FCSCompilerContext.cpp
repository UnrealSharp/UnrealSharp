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
	TSharedRef<FCSharpClassInfo> ClassInfo = ManagedClass->GetClassInfo();

	TArray<UCSFunctionBase*> Functions;
	FCSGeneratedClassBuilder::CreateFunctions(ManagedClass, ClassInfo->TypeMetaData.ToSharedRef(), Functions);

	// Add them to the skeleton class so they show up in the blueprint editor
	for (UCSFunctionBase* Function : Functions)
	{
		FCSFunctionFactory::AddFunctionToOuter(Blueprint->SkeletonGeneratedClass, Function);
	}
}

void FCSCompilerContext::CleanAndSanitizeClass(UBlueprintGeneratedClass* ClassToClean, UObject*& InOldCDO)
{
	FKismetCompilerContext::CleanAndSanitizeClass(ClassToClean, InOldCDO);

	// Re-apply the class flags from the managed class, they are lost during the cleanup process
	if (UCSClass* ManagedClass = Cast<UCSClass>(ClassToClean))
	{
		TSharedRef<FCSharpClassInfo> ClassInfo = ManagedClass->GetClassInfo();
		ManagedClass->ClassFlags |= ClassInfo->TypeMetaData->ClassFlags;
	}
}

#undef LOCTEXT_NAMESPACE


