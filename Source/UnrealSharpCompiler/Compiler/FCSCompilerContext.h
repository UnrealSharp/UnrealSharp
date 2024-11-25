#pragma once

#include "CoreMinimal.h"
#include "KismetCompiler.h"
#include "KismetCompilerModule.h"

class UCSBlueprint;
class UAdventureAssetBlueprint;

class FCSBlueprintCompiler : public IBlueprintCompiler
{
public:

	FCSBlueprintCompiler();

	// IBlueprintCompiler interface
	virtual bool CanCompile(const UBlueprint* Blueprint) override;
	virtual void Compile(UBlueprint* Blueprint, const FKismetCompilerOptions& CompileOptions, FCompilerResultsLog& Results) override;
	// End of IBlueprintCompiler interface
	
};

class FCSCompilerContext : public FKismetCompilerContext
{
public:

	FCSCompilerContext(UCSBlueprint* SourceAdventureBP, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompilerOptions);

	// FKismetCompilerContext interface
	virtual void CreateFunctionList() override;
	virtual void CleanAndSanitizeClass(UBlueprintGeneratedClass* ClassToClean, UObject*& OldCDO) override;
	// End of FKismetCompilerContext interface

protected:
	typedef FKismetCompilerContext Super;
};