#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FScriptSet)
{
	using FGetKeyHash = uint32(*)(const void*);
	using FEqualityFn = bool(*)(const void*, const void*);
	using FConstructFn = void(*)(void*);
	using FDestructFn = void(*)(void*);
	
	bool IsValidIndex(FScriptSet* ScriptSet, int32 Index)
	{
		return ScriptSet->IsValidIndex(Index);
	}

	int Num(FScriptSet* ScriptSet)
	{
		int Num = ScriptSet->Num();
		return Num;
	}

	int GetMaxIndex(FScriptSet* ScriptSet)
	{
		return ScriptSet->GetMaxIndex();
	}

	void* GetData(int Index, FScriptSet* ScriptSet, FSetProperty* Property)
	{
		return ScriptSet->GetData(Index, Property->SetLayout);
	}

	void Empty(int Slack, FScriptSet* ScriptSet, FSetProperty* Property)
	{
		return ScriptSet->Empty(Slack, Property->SetLayout);
	}

	void RemoveAt(int Index, FScriptSet* ScriptSet, FSetProperty* Property)
	{
		return ScriptSet->RemoveAt(Index, Property->SetLayout);
	}

	int AddUninitialized(FScriptSet* ScriptSet, FSetProperty* Property)
	{
		return ScriptSet->AddUninitialized(Property->SetLayout);
	}

	void Add(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn, FConstructFn ConstructFn, FDestructFn DestructFn)
	{
		ScriptSet->Add(Element, Property->SetLayout, GetKeyHash, EqualityFn, ConstructFn, DestructFn);
	}

	int32 FindOrAdd(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn, FConstructFn ConstructFn)
	{
		return ScriptSet->FindOrAdd(Element, Property->SetLayout, GetKeyHash, EqualityFn, ConstructFn);
	}

	int FindIndex(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn)
	{
		return ScriptSet->FindIndex(Element, Property->SetLayout, GetKeyHash, EqualityFn);
	}
	
	BIND_UNREALSHARP_FUNCTION(IsValidIndex)
	BIND_UNREALSHARP_FUNCTION(Num)
	BIND_UNREALSHARP_FUNCTION(GetMaxIndex)
	BIND_UNREALSHARP_FUNCTION(GetData)
	BIND_UNREALSHARP_FUNCTION(Empty)
	BIND_UNREALSHARP_FUNCTION(RemoveAt)
	BIND_UNREALSHARP_FUNCTION(AddUninitialized)
	BIND_UNREALSHARP_FUNCTION(Add)
	BIND_UNREALSHARP_FUNCTION(FindOrAdd)
	BIND_UNREALSHARP_FUNCTION(FindIndex)
}
