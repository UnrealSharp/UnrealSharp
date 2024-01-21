#pragma once

#include "CoreMinimal.h"

// Specifies the inclusions or non-inclusions for a code generator white-, grey-, or blacklist.
class FCSInclusionLists
{
public:
	void AddEnum(FName EnumName);
	bool HasEnum(const UEnum* Enum) const;
	void AddClass(FName ClassName);
	bool HasClass(const UClass* Class) const;
	void AddStruct(FName StructName);
	bool HasStruct(const UField* Struct) const;
	void AddAllFunctions(FName StructName);
	void AddFunction(FName StructName, FName FunctionName);
	void AddFunctionCategory(FName StructName, const FString& Category);
	bool HasFunction(const UStruct* Struct, const UFunction* Function) const;
	void AddOverridableFunction(FName StructName, FName OverridableFunctionName);
	bool HasOverridableFunction(const UStruct* Struct, const UFunction* OverridableFunction) const;
	void AddProperty(FName StructName, FName PropertyName);
	bool HasProperty(const UStruct* Struct, const FProperty* Property) const;

private:
	TSet<FName> Enumerations;
	TSet<FName> Classes;
	TSet<FName> Structs;
	TSet<FName> AllFunctions;
	TMap<FName, TSet<FString>> FunctionCategories;
	TMap<FName, TSet<FName>> Functions;
	TMap<FName, TSet<FName>> OverridableFunctions;
	TMap<FName, TSet<FName>> Properties;
};
