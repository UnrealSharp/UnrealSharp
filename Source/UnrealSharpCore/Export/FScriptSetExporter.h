#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FScriptSetExporter.generated.h"

using FGetKeyHash = uint32(*)(const void*);
using FEqualityFn = bool(*)(const void*, const void*);
using FConstructFn = void(*)(void*);
using FDestructFn = void(*)(void*);

UCLASS()
class UNREALSHARPCORE_API UFScriptSetExporter : public UObject
{
	GENERATED_BODY()

public:
	
	UNREALSHARP_FUNCTION()
	static bool IsValidIndex(FScriptSet* ScriptSet, int32 Index);

	UNREALSHARP_FUNCTION()
	static int Num(FScriptSet* ScriptSet);

	UNREALSHARP_FUNCTION()
	static int GetMaxIndex(FScriptSet* ScriptSet);

	UNREALSHARP_FUNCTION()
	static void* GetData(int Index, FScriptSet* ScriptSet, FSetProperty* Property);

	UNREALSHARP_FUNCTION()
	static void Empty(int Slack, FScriptSet* ScriptSet, FSetProperty* Property);

	UNREALSHARP_FUNCTION()
	static void RemoveAt(int Index, FScriptSet* ScriptSet, FSetProperty* Property);

	UNREALSHARP_FUNCTION()
	static int AddUninitialized(FScriptSet* ScriptSet, FSetProperty* Property);

	UNREALSHARP_FUNCTION()
	static void Add(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn, FConstructFn ConstructFn, FDestructFn DestructFn);

	UNREALSHARP_FUNCTION()
	static int32 FindOrAdd(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn, FConstructFn ConstructFn);

	UNREALSHARP_FUNCTION()
	static int FindIndex(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn);
	
	
};
