#pragma once

#include "CSharpForUE/TypeGenerator/CSFunction.h"

struct FPropertyMetaData;
struct FClassMetaData;
struct FFunctionMetaData;
class UClass;

class FCSFunctionFactory
{
public:
	
	static UCSFunction* CreateFunctionFromMetaData(UClass* Outer, const FFunctionMetaData& FunctionMetaData);
	static UCSFunction* CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction);
	
	static void GetOverriddenFunctions(const UClass* Outer, const TSharedRef<FClassMetaData>& ClassMetaData, TArray<UFunction*>& VirtualFunctions);
	static void GenerateVirtualFunctions(UClass* Outer, const TSharedRef<FClassMetaData>& ClassMetaData);
	static void GenerateFunctions(UClass* Outer, const TArray<FFunctionMetaData>& Functions);

	static UCSFunction* CreateFunction(
		UClass* Outer,
		const FName& Name,
		EFunctionFlags FunctionFlags = FUNC_None,
		UStruct* ParentFunction = nullptr,
		void* ManagedMethod = nullptr);

private:


	static void FinalizeFunctionSetup(UClass* Outer, UCSFunction* Function);
	
};
