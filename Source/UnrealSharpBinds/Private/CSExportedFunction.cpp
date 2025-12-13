#include "CSExportedFunction.h"

#include "CSBindsManager.h"

FCSExportedFunction::FCSExportedFunction(const FName& OuterName, const FName& Name, void* InFunctionPointer, int32 InParameterSize):
	Name(Name),
	FunctionPointer(InFunctionPointer),
	ParameterSize(InParameterSize)
{
	FCSBindsManager::RegisterExportedFunction(OuterName, *this);
}
