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
	FProperty* Property = FindFProperty<FProperty>(Struct, PropertyName);
	return Property;
}

int32 UFPropertyExporter::GetPropertyOffset(FProperty* Property)
{
	int32 Offset = Property->GetOffset_ForInternal();
	return Offset;
}

int32 UFPropertyExporter::GetPropertyOffsetFromName(UStruct* InStruct, const char* InPropertyName)
{
	const FProperty* FoundProperty = GetNativePropertyFromName(InStruct, InPropertyName);
	return FoundProperty->GetOffset_ForInternal();
}

int32 UFPropertyExporter::GetPropertyArrayDimFromName(UStruct* InStruct, const char* PropertyName)
{
	const FProperty* Property = GetNativePropertyFromName(InStruct, PropertyName);
	return Property->ArrayDim;
}
