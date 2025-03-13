#include "UnrealSharpBinds.h"

#include "CSBindsManager.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpBindsModule"

FCSExportedFunction::FCSExportedFunction(const FName& OuterName, const FName& FunctionName, void* InFunctionPointer,
	int32 InSize):
	FunctionName(FunctionName),
	FunctionPointer(InFunctionPointer),
	Size(InSize)
{
	UCSBindsManager::RegisterExportedFunction(OuterName, *this);
}

void FUnrealSharpBindsModule::StartupModule()
{
}

void FUnrealSharpBindsModule::ShutdownModule()
{

}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpBindsModule, UnrealSharpBinds)