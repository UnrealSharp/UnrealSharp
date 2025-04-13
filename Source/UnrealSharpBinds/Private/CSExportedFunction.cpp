#include "CSExportedFunction.h"

#include "CSBindsManager.h"

FCSExportedFunction::FCSExportedFunction(const FName& OuterName, const FName& Name, void* InFunctionPointer, int32 InSize):
	Name(Name),
	FunctionPointer(InFunctionPointer),
	Size(InSize)
{
	FCSBindsManager::RegisterExportedFunction(OuterName, *this);
}
