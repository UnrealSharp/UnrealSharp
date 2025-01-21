#pragma once

#include "CoreMinimal.h"
#include "KismetCompilerModule.h"

class UCSClass;
class UCSBlueprint;

class FCSBlueprintCompiler : public IBlueprintCompiler
{
public:
	// IBlueprintCompiler interface
	virtual bool CanCompile(const UBlueprint* Blueprint) override;
	virtual void Compile(UBlueprint* Blueprint, const FKismetCompilerOptions& CompileOptions, FCompilerResultsLog& Results) override;
	// End of IBlueprintCompiler interface
};