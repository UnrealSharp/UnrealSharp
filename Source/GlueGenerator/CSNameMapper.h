#pragma once

#include "CSharpGeneratorUtilities.h"

class FCSGenerator;
using namespace ScriptGeneratorUtilities;

class FCSNameMapper : public FScriptNameMapper
{
public:
	
	FCSNameMapper(FCSGenerator* ModuleFinder) : ScriptGenerator(ModuleFinder)
	{
	}

	virtual FString ScriptifyName(const FString& InName, const EScriptNameKind InNameKind) const override;

	FString GetQualifiedName(const UClass* Class) const;
	FString GetQualifiedName(const UScriptStruct* Struct) const;
	FString GetQualifiedName(const UEnum* Enum) const;

private:

	FCSGenerator* ScriptGenerator;
	
};