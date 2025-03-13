#include "CSBindsManager.h"

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

void* UCSBindsManager::GetBoundNativeFunction(TCHAR* InOuterName, TCHAR* InFunctionName, int32 ManagedFunctionSize)
{
	UCSBindsManager* Instance = Get();
	FName ManagedOuterName = FName(InOuterName);
	FName ManagedFunctionName = FName(InFunctionName);
	
	TArray<FCSExportedFunction>* ExportedFunctions = Instance->ExportedFunctionsMap.Find(ManagedOuterName);
	
	if (!ExportedFunctions)
	{
		UE_LOG(LogTemp, Error, TEXT("No exported functions found for %s"), InOuterName);
		return nullptr;
	}

	for (FCSExportedFunction& NativeFunction : *ExportedFunctions)
	{
		if (NativeFunction.FunctionName != ManagedFunctionName)
		{
			continue;
		}
			
		if (NativeFunction.Size != ManagedFunctionSize)
		{
			UE_LOG(LogTemp, Fatal, TEXT("Function size mismatch for %s::%s"), InOuterName, InFunctionName);
			return nullptr;
		}
			
		return NativeFunction.FunctionPointer;
	}

	UE_LOG(LogTemp, Error, TEXT("No function found for %s.%s"), InOuterName, InFunctionName);
	return nullptr;
}
