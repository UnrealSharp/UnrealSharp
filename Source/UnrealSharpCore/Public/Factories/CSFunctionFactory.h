#pragma once

#include "ReflectionData/CSClassReflectionData.h"
#include "Functions/CSFunction.h"

class UCSBlueprint;
class UClass;

class FCSFunctionFactory
{
public:
	
	static UCSFunctionBase* CreateFunctionFromReflectionData(UClass* Outer, const FCSFunctionReflectionData& FunctionReflectionData);
	static UCSFunctionBase* CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction);
	
	static void GetOverriddenFunctions(const UClass* Outer, const TSharedPtr<const FCSClassReflectionData>& ClassReflectionData, TArray<UFunction*>& VirtualFunctions);
	UNREALSHARPCORE_API static void GenerateVirtualFunctions(UClass* Outer, const TSharedPtr<const FCSClassReflectionData>& ClassReflectionData);
	UNREALSHARPCORE_API static void GenerateFunctions(UClass* Outer, const TArray<FCSFunctionReflectionData>& FunctionsReflectionData);

	static void AddFunctionToOuter(UClass* Outer, UCSFunctionBase* Function);
	
	static FProperty* CreateParameter(UFunction* Function, const FCSPropertyReflectionData& PropertyReflectionData);
	static void CreateParameters(UFunction* Function, const FCSFunctionReflectionData& PropertyReflectionData);
	
private:
	static void FinalizeFunctionSetup(UClass* Outer, UCSFunctionBase* Function);
	static UCSFunctionBase* CreateFunction_Internal(UClass* Outer, const FName& Name, const FCSFunctionReflectionData& FunctionReflectionData, EFunctionFlags FunctionFlags = FUNC_None, UStruct* ParentFunction = nullptr);
};
