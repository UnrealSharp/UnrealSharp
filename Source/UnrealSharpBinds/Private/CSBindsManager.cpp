#include "CSBindsManager.h"
#include "UnrealSharpBinds.h"

FCSBindsManager* FCSBindsManager::BindsManagerInstance = nullptr;

FCSBindsManager* FCSBindsManager::Get()
{
	if (!BindsManagerInstance)
	{
		BindsManagerInstance = new FCSBindsManager();
	}

	return BindsManagerInstance;
}

void FCSBindsManager::RegisterExportedFunction(const FName& ClassName, const FCSExportedFunction& ExportedFunction)
{
	FCSBindsManager* Instance = Get();
	TArray<FCSExportedFunction>& ExportedFunctions = Instance->ExportedFunctionsMap.FindOrAdd(ClassName);
	ExportedFunctions.Add(ExportedFunction);
}

void* FCSBindsManager::GetBoundFunction(const TCHAR* InOuterName, const TCHAR* InFunctionName, int32 ManagedFunctionSize)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSBindsManager::GetBoundFunction);
	
	FCSBindsManager* Instance = Get();
	FName ManagedOuterName = FName(InOuterName);
	FName ManagedFunctionName = FName(InFunctionName);
	
	TArray<FCSExportedFunction>* ExportedFunctions = Instance->ExportedFunctionsMap.Find(ManagedOuterName);

	if (!ExportedFunctions)
	{
		UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No exported functions found for %s"), InOuterName);
		return nullptr;
	}

	for (FCSExportedFunction& NativeFunction : *ExportedFunctions)
	{
		if (NativeFunction.Name != ManagedFunctionName)
		{
			continue;
		}
			
		if (NativeFunction.Size != ManagedFunctionSize)
		{
			UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: Function size mismatch for %s::%s."), InOuterName, InFunctionName);
			return nullptr;
		}
			
		return NativeFunction.FunctionPointer;
	}

	UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No function found for %s.%s"), *ManagedOuterName.ToString(), *ManagedFunctionName.ToString());
	return nullptr;
}
