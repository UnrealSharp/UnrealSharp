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

#if PLATFORM_WINDOWS
void* FCSBindsManager::GetBoundFunction(const TCHAR* InOuterName, const TCHAR* InFunctionName, int32 ManagedFunctionSize)
#else
void* FCSBindsManager::GetBoundFunction(const char* InOuterName, const char* InFunctionName, int32 ManagedFunctionSize)
#endif
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSBindsManager::GetBoundFunction);
	
	FCSBindsManager* Instance = Get();
	FName ManagedOuterName = FName(InOuterName);
	FName ManagedFunctionName = FName(InFunctionName);
	
	TArray<FCSExportedFunction>* ExportedFunctions = Instance->ExportedFunctionsMap.Find(ManagedOuterName);

	if (!ExportedFunctions)
	{
		UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No exported functions found for %s"), *ManagedOuterName.ToString());
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
			UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: Function size mismatch for %s::%s."), *ManagedOuterName.ToString(), *ManagedFunctionName.ToString());
			return nullptr;
		}
			
		return NativeFunction.FunctionPointer;
	}

	UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No function found for %s.%s"), *ManagedOuterName.ToString(), *ManagedFunctionName.ToString());
	return nullptr;
}
