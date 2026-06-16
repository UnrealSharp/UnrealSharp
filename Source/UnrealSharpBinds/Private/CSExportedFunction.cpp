#include "CSExportedFunction.h"
#include "CSBindsRegistry.h"

FCSBoundFunction::FCSBoundFunction(const FName& OuterName, const FName& Name, void* InFunctionPointer, int32 InParameterSize) :
	Name(Name),
	ParameterSize(InParameterSize),
	FunctionPointer(InFunctionPointer)
{
	FCSBindsRegistry::RegisterBoundFunction(OuterName, *this);
}
