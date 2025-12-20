#include "CSBindsManager.h"
#include "UnrealSharpBinds.h"
#include "Logging/StructuredLog.h"

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
	TArray<FCSExportedFunction>& ExportedFunctions = Instance->ExportedFunctions.FindOrAdd(ClassName);
	ExportedFunctions.Add(ExportedFunction);
}

void* FCSBindsManager::GetBoundFunction(const TCHAR* InOuterName, const TCHAR* InFunctionName, int32 InParametersSize)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSBindsManager::GetBoundFunction);
	
	FCSBindsManager* Instance = Get();
	FName ManagedOuterName = FName(InOuterName);
	FName ManagedFunctionName = FName(InFunctionName);
	
	TArray<FCSExportedFunction>* ExportedFunctions = Instance->ExportedFunctions.Find(ManagedOuterName);

	if (!ExportedFunctions)
	{
		UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No exported functions found for %s"), InOuterName);
		return nullptr;
	}

	void* FunctionPointer = nullptr;
	for (FCSExportedFunction& NativeFunction : *ExportedFunctions)
	{
		if (NativeFunction.Name != ManagedFunctionName)
		{
			continue;
		}
			
		if (NativeFunction.ParameterSize != InParametersSize)
		{
			UE_LOGFMT(LogUnrealSharpBinds, Error, "Failed to get BoundNativeFunction: Function size mismatch for {0}.{1} (expected {2}, got {3})",
				*ManagedOuterName.ToString(), *ManagedFunctionName.ToString(), NativeFunction.ParameterSize, InParametersSize);
			
			break;
		}
			
		FunctionPointer = NativeFunction.FunctionPointer;
		break;
	}
	
	if (!FunctionPointer)
	{
		UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No function found for %s.%s"), *ManagedOuterName.ToString(), *ManagedFunctionName.ToString());
		return nullptr;
	}
	
	return FunctionPointer;
}
