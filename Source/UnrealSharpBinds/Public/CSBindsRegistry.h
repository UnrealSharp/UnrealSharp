#pragma once

#include "CSExportedFunction.h"

#define DECLARE_UNREALSHARP_BINDER(Name) \
namespace Name { static const FName UnrealSharpBinderName(#Name); } \
namespace Name

#define BIND_UNREALSHARP_FUNCTION(FunctionName) \
static const FCSBoundFunction ANONYMOUS_VARIABLE(ZUnrealSharpBind_) = FCSBoundFunction( \
UnrealSharpBinderName, \
FName(#FunctionName), \
(void*)&FunctionName, \
static_cast<int32>(GetFunctionSize(&FunctionName)));

class FCSBindsRegistry
{
public:
	UNREALSHARPBINDS_API static void* GetBoundFunction(const TCHAR* BinderName, const TCHAR* InFunctionName, int32 InParametersSize);
	
	static void RegisterBoundFunction(const FName& BinderName, const FCSBoundFunction& ExportedFunction);
	static void DumpBoundFunctions(const TArray<FString>& Args);
	static void DumpBoundFunctionsForBinder(const TArray<FString>& Args);
private:
	static void DumpBoundFunctionsInternal(const FName& BinderName);
	static TMap<FName, TArray<FCSBoundFunction>> BinderToFunctionsMap;
};
