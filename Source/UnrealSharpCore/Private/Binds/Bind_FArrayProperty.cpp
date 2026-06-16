#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FArrayProperty)
{
	void InitializeArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int Length)
	{
		FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
		Helper.EmptyAndAddValues(Length);
	}

	void EmptyArray(FArrayProperty* ArrayProperty, const void* ScriptArray)
	{
		FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
		Helper.EmptyValues();
	}

	void AddToArray(FArrayProperty* ArrayProperty, const void* ScriptArray)
	{
		FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
		Helper.AddValue();
	}

	void InsertInArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int index)
	{
		FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
		Helper.InsertValues(index);
	}

	void RemoveFromArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int index)
	{
		FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
		Helper.RemoveValues(index);
	}

	void ResizeArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int Length)
	{
		FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
		Helper.Resize(Length);
	}

	void SwapValues(FArrayProperty* ArrayProperty, const void* ScriptArray, int indexA, int indexB)
	{
		FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
		Helper.SwapValues(indexA, indexB);
	}
	
	BIND_UNREALSHARP_FUNCTION(InitializeArray)
	BIND_UNREALSHARP_FUNCTION(EmptyArray)
	BIND_UNREALSHARP_FUNCTION(AddToArray)
	BIND_UNREALSHARP_FUNCTION(InsertInArray)
	BIND_UNREALSHARP_FUNCTION(RemoveFromArray)
	BIND_UNREALSHARP_FUNCTION(ResizeArray)
	BIND_UNREALSHARP_FUNCTION(SwapValues)
}
