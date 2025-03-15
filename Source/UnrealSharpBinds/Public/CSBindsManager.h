#pragma once

#include "CSExportedFunction.h"

#define UNREALSHARP_FUNCTION()

class FCSBindsManager
{
public:

	FCSBindsManager() = default;
	
	static FCSBindsManager* Get();
	
	UNREALSHARPBINDS_API static void RegisterExportedFunction(const FName& ClassName, const FCSExportedFunction& ExportedFunction);
	UNREALSHARPBINDS_API static void* GetBoundFunction(TCHAR* OuterName, TCHAR* FunctionName, int32 ManagedFunctionSize);
	
private:
	static FCSBindsManager* BindsManagerInstance;
	TMap<FName, TArray<FCSExportedFunction>> ExportedFunctionsMap;
};
