#pragma once

#include "CSExportedFunction.h"

// Native bound function. If you want to bind a function to C#, use this macro.
// The managed delegate signature must match the native function signature + outer name, and all params need to be blittable.
#define UNREALSHARP_FUNCTION()

class FCSBindsManager
{
public:
	
	static FCSBindsManager* Get();
	
	UNREALSHARPBINDS_API static void RegisterExportedFunction(const FName& ClassName, const FCSExportedFunction& ExportedFunction);

	UNREALSHARPBINDS_API static void* GetBoundFunction(const TCHAR* InOuterName, const TCHAR* InFunctionName, int32 ManagedFunctionSize);

private:
	FCSBindsManager() = default;
	static FCSBindsManager* BindsManagerInstance;
	TMap<FName, TArray<FCSExportedFunction>> ExportedFunctionsMap;
};
