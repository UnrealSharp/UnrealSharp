#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FScriptSetExporter.generated.h"

using FGetKeyHash = uint32(*)(const void*);
using FEqualityFn = bool(*)(const void*, const void*);
using FConstructFn = void(*)(void*);
using FDestructFn = void(*)(void*);

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UFScriptSetExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End of UFunctionsExporter interface

private:

	static bool IsValidIndex(FScriptSet* ScriptSet, int32 Index);
	static int Num(FScriptSet* ScriptSet);
	static int GetMaxIndex(FScriptSet* ScriptSet);
	static void* GetData(int Index, FScriptSet* ScriptSet, FSetProperty* Property);
	static void Empty(int Slack, FScriptSet* ScriptSet, FSetProperty* Property);
	static void RemoveAt(int Index, FScriptSet* ScriptSet, FSetProperty* Property);
	static int AddUninitialized(FScriptSet* ScriptSet, FSetProperty* Property);
	
	static void Add(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn, FConstructFn ConstructFn, FDestructFn DestructFn);
	static int32 FindOrAdd(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn, FConstructFn ConstructFn);
	static int FindIndex(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn);
	
	
};
