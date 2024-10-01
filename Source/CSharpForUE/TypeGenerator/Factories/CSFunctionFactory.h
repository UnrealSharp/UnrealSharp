#pragma once

#include "CSharpForUE/TypeGenerator/Functions/CSFunction.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"

class UClass;

class FCSFunctionFactory
{
public:
	
	static UCSFunctionBase* CreateFunctionFromMetaData(UClass* Outer, const FCSFunctionMetaData& FunctionMetaData);
	static UCSFunctionBase* CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction);
	
	static void GetOverriddenFunctions(const UClass* Outer, const TSharedRef<FCSClassMetaData>& ClassMetaData, TArray<UFunction*>& VirtualFunctions);
	static void GenerateVirtualFunctions(UClass* Outer, const TSharedRef<FCSClassMetaData>& ClassMetaData);
	static void GenerateFunctions(UClass* Outer, const TArray<FCSFunctionMetaData>& Functions);

	static UCSFunctionBase* CreateFunction(
		UClass* Outer,
		const FName& Name,
		const FCSFunctionMetaData& FunctionMetaData,
		EFunctionFlags FunctionFlags = FUNC_None,
		UStruct* ParentFunction = nullptr);

private:

	static FProperty* CreateProperty(UCSFunctionBase* Function, const FCSPropertyMetaData& PropertyMetaData);
	static void FinalizeFunctionSetup(UClass* Outer, UCSFunctionBase* Function);
	
};
