#include "CSBindsRegistry.h"
#include "UnrealSharpBinds.h"
#include "Logging/StructuredLog.h"

static FAutoConsoleCommand CVarDumpBoundFunctions(
	TEXT("UnrealSharp.DumpBoundFunctions"),
	TEXT("Dumps all currently bound functions for all binders to the log."),
	FConsoleCommandWithArgsDelegate::CreateStatic(&FCSBindsRegistry::DumpBoundFunctions)
);

static FAutoConsoleCommand CVarDumpBoundFunctionsForBinder(
	TEXT("UnrealSharp.DumpBoundFunctionsForBinder"),
	TEXT("Dumps all currently exported functions for a specific binder to the log. Usage: UnrealSharp.DumpBoundFunctionsForBinder Bind_FVector"),
	FConsoleCommandWithArgsDelegate::CreateStatic(&FCSBindsRegistry::DumpBoundFunctionsForBinder)
);

TMap<FName, TArray<FCSBoundFunction>> FCSBindsRegistry::BinderToFunctionsMap;

void* FCSBindsRegistry::GetBoundFunction(const TCHAR* BinderName, const TCHAR* InFunctionName, int32 InParametersSize)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSBindsManager::GetBoundFunction);
	
	FName ManagedOuterName = FName(BinderName);
	FName ManagedFunctionName = FName(InFunctionName);
	
	TArray<FCSBoundFunction>* BoundFunctions = BinderToFunctionsMap.Find(ManagedOuterName);

	if (!BoundFunctions)
	{
		UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No exported functions found for %s"), BinderName);
		return nullptr;
	}

	void* FunctionPointer = nullptr;
	for (FCSBoundFunction& NativeFunction : *BoundFunctions)
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

void FCSBindsRegistry::RegisterBoundFunction(const FName& BinderName, const FCSBoundFunction& ExportedFunction)
{
	TArray<FCSBoundFunction>& ExportedFunctions = BinderToFunctionsMap.FindOrAdd(BinderName);
	ExportedFunctions.Add(ExportedFunction);
}

void FCSBindsRegistry::DumpBoundFunctions(const TArray<FString>& Args)
{
	UE_LOG(LogUnrealSharpBinds, Log, TEXT("Dumping exported functions:"));
	
	for (const TPair<FName, TArray<FCSBoundFunction>>& ExportedFunctionsKVP : BinderToFunctionsMap)
	{
		DumpBoundFunctionsInternal(ExportedFunctionsKVP.Key);
	}
}

void FCSBindsRegistry::DumpBoundFunctionsForBinder(const TArray<FString>& Args)
{
	if (Args.Num() == 0)
	{
		UE_LOG(LogUnrealSharpBinds, Warning, TEXT("Please provide a binder name."));
		return;
	}
	
	FName BinderName = FName(*Args[0]);
	DumpBoundFunctionsInternal(BinderName);
}

void FCSBindsRegistry::DumpBoundFunctionsInternal(const FName& BinderName)
{
	TArray<FCSBoundFunction> BoundFunctions = BinderToFunctionsMap.FindRef(BinderName);
    
	if (BoundFunctions.IsEmpty())
	{
		UE_LOGFMT(LogUnrealSharpBinds, Warning, "No bound functions found for binder {0}", *BinderName.ToString());
		return;
	}
	
	UE_LOGFMT(LogUnrealSharpBinds, Log, "{0}", *BinderName.ToString());
	
	BoundFunctions.Sort([](const FCSBoundFunction& A, const FCSBoundFunction& B) 
	{
		return A.Name.ToString() < B.Name.ToString();
	});

	for (const FCSBoundFunction& ExportedFunction : BoundFunctions)
	{
		UE_LOGFMT(LogUnrealSharpBinds, Log, "	{0} | Size: {1}", *ExportedFunction.Name.ToString(), ExportedFunction.ParameterSize);
	}
}
