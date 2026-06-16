#include "CSBindsRegistry.h"
#include "UnrealSharpBinds.h"
#include "Logging/StructuredLog.h"

void DumpBoundFunctionsInternal(const FName& BinderName)
{
	TArray<FCSBoundFunction> BoundFunctions = FCSBindsRegistry::GetBinderToFunctionsMap().FindRef(BinderName);
    
	if (BoundFunctions.IsEmpty())
	{
		UE_LOGFMT(LogUnrealSharpBinds, Warning, "No bound functions found for binder {0}", *BinderName.ToString());
		return;
	}
	
	UE_LOGFMT(LogUnrealSharpBinds, Log, "{0}", *BinderName.ToString());
	
	BoundFunctions.Sort([](const FCSBoundFunction& A, const FCSBoundFunction& B) 
	{
		return A.FunctionName.ToString() < B.FunctionName.ToString();
	});

	for (const FCSBoundFunction& ExportedFunction : BoundFunctions)
	{
		UE_LOGFMT(LogUnrealSharpBinds, Log, "	{0} | Size: {1}", *ExportedFunction.FunctionName.ToString(), ExportedFunction.ParameterSize);
	}
}

void DumpBoundFunctions(const TArray<FString>& Args)
{
	UE_LOG(LogUnrealSharpBinds, Log, TEXT("Dumping exported functions:"));
	
	for (const TPair<FName, TArray<FCSBoundFunction>>& ExportedFunctionsKVP : FCSBindsRegistry::GetBinderToFunctionsMap())
	{
		DumpBoundFunctionsInternal(ExportedFunctionsKVP.Key);
	}
}

void DumpBoundFunctionsForBinder(const TArray<FString>& Args)
{
	if (Args.IsEmpty())
	{
		UE_LOG(LogUnrealSharpBinds, Warning, TEXT("Please provide a binder name."));
		return;
	}
	
	FName BinderName = FName(*Args[0]);
	DumpBoundFunctionsInternal(BinderName);
}

static FAutoConsoleCommand CVarDumpBoundFunctions(
	TEXT("UnrealSharp.DumpBoundFunctions"),
	TEXT("Dumps all currently bound functions for all binders to the log."),
	FConsoleCommandWithArgsDelegate::CreateStatic(&DumpBoundFunctions)
);

static FAutoConsoleCommand CVarDumpBoundFunctionsForBinder(
	TEXT("UnrealSharp.DumpBoundFunctionsForBinder"),
	TEXT("Dumps all currently exported functions for a specific binder to the log. Usage: UnrealSharp.DumpBoundFunctionsForBinder Bind_FVector"),
	FConsoleCommandWithArgsDelegate::CreateStatic(&DumpBoundFunctionsForBinder)
);

TMap<FName, TArray<FCSBoundFunction>> FCSBindsRegistry::BinderToFunctionsMap;

const FCSBoundFunction& FCSBindsRegistry::RegisterBoundFunction(const FName& BinderName, const FName& FunctionName, void* FunctionPointer, int32 ParameterSize)
{
	TArray<FCSBoundFunction>& ExportedFunctions = BinderToFunctionsMap.FindOrAdd(BinderName);
	const FCSBoundFunction& BoundFunction = ExportedFunctions.Emplace_GetRef(FunctionName, ParameterSize, FunctionPointer);
	
	UE_LOGFMT(LogUnrealSharpBinds, Verbose, "Registered bound function {0}.{1} with parameter size {2}", *BinderName.ToString(), *FunctionName.ToString(), ParameterSize);
	return BoundFunction;
}

void* FCSBindsRegistry::GetBoundFunction(const TCHAR* BinderName, const TCHAR* FunctionName, int32 ParameterSize)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSBindsManager::GetBoundFunction);
	
	FName ManagedOuterName = FName(BinderName);
	FName ManagedFunctionName = FName(FunctionName);
	
	TArray<FCSBoundFunction>* BoundFunctions = BinderToFunctionsMap.Find(ManagedOuterName);

	if (!BoundFunctions)
	{
		UE_LOG(LogUnrealSharpBinds, Error, TEXT("Failed to get BoundNativeFunction: No exported functions found for %s"), BinderName);
		return nullptr;
	}

	void* FunctionPointer = nullptr;
	for (const FCSBoundFunction& NativeFunction : *BoundFunctions)
	{
		if (NativeFunction.FunctionName != ManagedFunctionName)
		{
			continue;
		}
			
		if (NativeFunction.ParameterSize != ParameterSize)
		{
			UE_LOGFMT(LogUnrealSharpBinds, Error, "Failed to get BoundNativeFunction: Function size mismatch for {0}.{1} (expected {2}, got {3})",
				*ManagedOuterName.ToString(), *ManagedFunctionName.ToString(), NativeFunction.ParameterSize, ParameterSize);
			
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
