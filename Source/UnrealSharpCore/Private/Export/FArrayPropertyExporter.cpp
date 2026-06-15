#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FArrayPropertyExporter)
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
	
	EXPORT_UNREALSHARP_FUNCTION(InitializeArray)
	EXPORT_UNREALSHARP_FUNCTION(EmptyArray)
	EXPORT_UNREALSHARP_FUNCTION(AddToArray)
	EXPORT_UNREALSHARP_FUNCTION(InsertInArray)
	EXPORT_UNREALSHARP_FUNCTION(RemoveFromArray)
	EXPORT_UNREALSHARP_FUNCTION(ResizeArray)
	EXPORT_UNREALSHARP_FUNCTION(SwapValues)
}
