#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FScriptMapHelper)
{
	void AddPair(FMapProperty* MapProperty, const void* Address, const void* Key, const void* Value)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		Helper.AddPair(Key, Value);
	}

	void* FindOrAdd(FMapProperty* MapProperty, const void* Address, const void* Key)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		return Helper.FindOrAdd(Key);
	}

	int Num(FMapProperty* MapProperty, const void* Address)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		return Helper.Num();
	}

	int FindMapPairIndexFromHash(FMapProperty* MapProperty, const void* Address, const void* Key)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		return Helper.FindMapPairIndexFromHash(Key);
	}

	void RemoveIndex(FMapProperty* MapProperty, const void* Address, int Index)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		Helper.RemoveAt(Index);
	}

	void EmptyValues(FMapProperty* MapProperty, const void* Address)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		Helper.EmptyValues();
	}

	void Remove(FMapProperty* MapProperty, const void* Address, const void* Key)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		Helper.RemovePair(Key);
	}

	bool IsValidIndex(FMapProperty* MapProperty, const void* Address, int Index)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		return Helper.IsValidIndex(Index);
	}

	int GetMaxIndex(FMapProperty* MapProperty, const void* Address)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		return Helper.GetMaxIndex();
	}

	void* GetPairPtr(FMapProperty* MapProperty, const void* Address, int Index)
	{
		FScriptMapHelper Helper(MapProperty, Address);
		return Helper.GetPairPtr(Index);
	}
	
	BIND_UNREALSHARP_FUNCTION(AddPair)
	BIND_UNREALSHARP_FUNCTION(FindOrAdd)
	BIND_UNREALSHARP_FUNCTION(Num)
	BIND_UNREALSHARP_FUNCTION(FindMapPairIndexFromHash)
	BIND_UNREALSHARP_FUNCTION(RemoveIndex)
	BIND_UNREALSHARP_FUNCTION(EmptyValues)
	BIND_UNREALSHARP_FUNCTION(Remove)
	BIND_UNREALSHARP_FUNCTION(IsValidIndex)
	BIND_UNREALSHARP_FUNCTION(GetMaxIndex)
	BIND_UNREALSHARP_FUNCTION(GetPairPtr)
}
