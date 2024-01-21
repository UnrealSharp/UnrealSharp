#include "FPropertyExporter.h"

void UFPropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetNativePropertyFromName)
	EXPORT_FUNCTION(GetPropertyOffsetFromName)
	EXPORT_FUNCTION(GetPropertyArrayDimFromName)
}

FProperty* UFPropertyExporter::GetNativePropertyFromName(UStruct* Struct, const char* PropertyName)
{
	FProperty* Property = FindFProperty<FProperty>(Struct, PropertyName);
	return Property;
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
