#include "CSBindsManager.h"

#include "UnrealSharpBinds.h"

UCSBindsManager* UCSBindsManager::BindsManagerInstance = nullptr;

UCSBindsManager* UCSBindsManager::Get()
{
	if (!BindsManagerInstance)
	{
		constexpr EObjectFlags ObjectFlags = RF_Public | RF_MarkAsRootSet;
		BindsManagerInstance = NewObject<UCSBindsManager>(GetTransientPackage(), TEXT("CSBindsManager"), ObjectFlags);
	}

	return BindsManagerInstance;
}

void UCSBindsManager::RegisterExportedFunction(const FName& ClassName, const FCSExportedFunction& ExportedFunction)
{
	UCSBindsManager* Instance = Get();
	TArray<FCSExportedFunction>& ExportedFunctions = Instance->ExportedFunctionsMap.FindOrAdd(ClassName);
	ExportedFunctions.Add(ExportedFunction);
}

void* UCSBindsManager::GetBoundFunction(TCHAR* InOuterName, TCHAR* InFunctionName, int32 ManagedFunctionSize)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSBindsManager::GetBoundFunction);
	
	UCSBindsManager* Instance = Get();
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

	UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No function found for %s.%s"), InOuterName, InFunctionName);
	return nullptr;
}
