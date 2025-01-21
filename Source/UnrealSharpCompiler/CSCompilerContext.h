#pragma once
#include "KismetCompiler.h"

class UCSClass;
class UCSBlueprint;

class FCSCompilerContext : public FKismetCompilerContext
{
public:

	FCSCompilerContext(UCSBlueprint* Blueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompilerOptions);

	// FKismetCompilerContext interface
	virtual void FinishCompilingClass(UClass* Class) override;
	virtual void CreateFunctionList() override;
	virtual void CreateClassVariablesFromBlueprint() override;
	virtual void CleanAndSanitizeClass(UBlueprintGeneratedClass* ClassToClean, UObject*& OldCDO) override;
	virtual void SpawnNewClass(const FString& NewClassName) override;
	// End of FKismetCompilerContext interface
protected:
	typedef FKismetCompilerContext Super;
private:
	void GenerateFunctions() const;
	UCSClass* GetMainClass() const;
};

