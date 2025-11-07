#include "CSBlueprintCompiler.h"
#include "CSCompilerContext.h"
#include "Types/CSBlueprint.h"

bool FCSBlueprintCompiler::CanCompile(const UBlueprint* Blueprint)
{
	return Blueprint->IsA<UCSBlueprint>();
}

void FCSBlueprintCompiler::Compile(UBlueprint* Blueprint, const FKismetCompilerOptions& CompileOptions, FCompilerResultsLog& Results)
{
	if (UCSBlueprint* CSBlueprint = CastChecked<UCSBlueprint>(Blueprint))
	{
		FCSCompilerContext Compiler(CSBlueprint, Results, CompileOptions);
		Compiler.Compile();
	}
}
