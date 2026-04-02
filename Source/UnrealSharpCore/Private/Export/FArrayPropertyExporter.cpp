#include "Export/FArrayPropertyExporter.h"

void UFArrayPropertyExporter::InitializeArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int Length)
{
	FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
	Helper.EmptyAndAddValues(Length);
}

void UFArrayPropertyExporter::EmptyArray(FArrayProperty* ArrayProperty, const void* ScriptArray)
{
	FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
	Helper.EmptyValues();
}

void UFArrayPropertyExporter::AddToArray(FArrayProperty* ArrayProperty, const void* ScriptArray)
{
	FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
	Helper.AddValue();
}

void UFArrayPropertyExporter::InsertInArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int index)
{
	FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
	Helper.InsertValues(index);
}

void UFArrayPropertyExporter::RemoveFromArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int index)
{
	FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
	Helper.RemoveValues(index);
}

void UFArrayPropertyExporter::ResizeArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int Length)
{
	FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
	Helper.Resize(Length);
}

void UFArrayPropertyExporter::SwapValues(FArrayProperty* ArrayProperty, const void* ScriptArray, int indexA, int indexB)
{
	FScriptArrayHelper Helper(ArrayProperty, ScriptArray);
	Helper.SwapValues(indexA, indexB);
}
