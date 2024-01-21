#include "FArrayPropertyExporter.h"

void UFArrayPropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(EmptyArray)
	EXPORT_FUNCTION(AddToArray)
	EXPORT_FUNCTION(InsertInArray)
	EXPORT_FUNCTION(RemoveFromArray)
	EXPORT_FUNCTION(GetArrayElementSize)
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

int32 UFArrayPropertyExporter::GetArrayElementSize(UStruct* Struct, const char* PropertyName)
{
	check(Struct);
	const FArrayProperty* ArrayProperty = FindFProperty<FArrayProperty>(Struct, *FString(PropertyName));
	const FProperty* InnerProperty = ArrayProperty->Inner;
	check(InnerProperty);
	return InnerProperty->GetSize();
}
