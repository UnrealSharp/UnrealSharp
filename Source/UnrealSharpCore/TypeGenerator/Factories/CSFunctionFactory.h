#pragma once

#include "UnrealSharpCore/TypeGenerator/Functions/CSFunction.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"

class UCSBlueprint;
class UClass;

class UNREALSHARPCORE_API FCSFunctionFactory
{
public:
	
	static UCSFunctionBase* CreateFunctionFromMetaData(UClass* Outer, const FCSFunctionMetaData& FunctionMetaData);
	static UCSFunctionBase* CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction);
	
	static void GetOverriddenFunctions(const UClass* Outer, const TSharedPtr<const FCSClassMetaData>& ClassMetaData, TArray<UFunction*>& VirtualFunctions);
	static void GenerateVirtualFunctions(UClass* Outer, const TSharedPtr<const FCSClassMetaData>& ClassMetaData);
	static void GenerateFunctions(UClass* Outer, const TArray<FCSFunctionMetaData>& FunctionsMetaData);

	static void AddFunctionToOuter(UClass* Outer, UCSFunctionBase* Function);

	static UCSFunctionBase* CreateFunction(
		UClass* Outer,
		const FName& Name,
		const FCSFunctionMetaData& FunctionMetaData,
		EFunctionFlags FunctionFlags = FUNC_None,
		UStruct* ParentFunction = nullptr);

	static void FinalizeFunctionSetup(UClass* Outer, UCSFunctionBase* Function);
	
	static FProperty* CreateParameter(UFunction* Function, const FCSPropertyMetaData& PropertyMetaData);
	static void CreateParameters(UFunction* Function, const FCSFunctionMetaData& FunctionMetaData);
};
