#include "FPropertyExporter.h"

void UFPropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetNativePropertyFromName)
	EXPORT_FUNCTION(GetPropertyOffsetFromName)
	EXPORT_FUNCTION(GetPropertyArrayDimFromName)
	EXPORT_FUNCTION(GetPropertyOffset)
}

FProperty* UFPropertyExporter::GetNativePropertyFromName(UStruct* Struct, const char* PropertyName)
{
	check(Struct)
	FProperty* Property = FindFProperty<FProperty>(Struct, PropertyName);
	check(Property)
	return Property;
}

int32 UFPropertyExporter::GetPropertyOffset(FProperty* Property)
{
	check(Property)
	int32 Offset = Property->GetOffset_ForInternal();
	return Offset;
}

int32 UFPropertyExporter::GetPropertyOffsetFromName(UStruct* InStruct, const char* InPropertyName)
{
	check(InStruct)
	FProperty* FoundProperty = GetNativePropertyFromName(InStruct, InPropertyName);
	check(FoundProperty)
	
	return FoundProperty->GetOffset_ForInternal();
}

int32 UFPropertyExporter::GetPropertyArrayDimFromName(UStruct* InStruct, const char* PropertyName)
{
	check(InStruct)
	const FProperty* Property = GetNativePropertyFromName(InStruct, PropertyName);
	check(Property)
	
	return Property->ArrayDim;
}
