#pragma once

#include "CSExportedFunction.h"

#define DECLARE_UNREALSHARP_EXPORTER(Name) \
namespace Name { static const FName UnrealSharpBinderName(#Name); } \
namespace Name

#define EXPORT_UNREALSHARP_FUNCTION(FunctionName) \
static const FCSExportedFunction ANONYMOUS_VARIABLE(ZUnrealSharpBind_) = FCSExportedFunction( \
UnrealSharpBinderName, \
FName(#FunctionName), \
(void*)&FunctionName, \
static_cast<int32>(GetFunctionSize(&FunctionName)));

class FCSBindsManager
{
public:
	
	UNREALSHARPBINDS_API static void RegisterExportedFunction(const FName& ClassName, const FCSExportedFunction& ExportedFunction);
	UNREALSHARPBINDS_API static void* GetBoundFunction(const TCHAR* InOuterName, const TCHAR* InFunctionName, int32 InParametersSize);

private:
	FCSBindsManager() = default;
	
	static FCSBindsManager* Get();
	
	static FCSBindsManager* BindsManagerInstance;
	TMap<FName, TArray<FCSExportedFunction>> ExportedFunctions;
};
