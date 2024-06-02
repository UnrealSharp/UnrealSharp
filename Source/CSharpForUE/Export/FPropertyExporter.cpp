#include "FPropertyExporter.h"

void UFPropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetNativePropertyFromName)
	EXPORT_FUNCTION(GetPropertyOffsetFromName)
	EXPORT_FUNCTION(GetPropertyArrayDimFromName)
	EXPORT_FUNCTION(GetPropertyOffset)
	EXPORT_FUNCTION(GetSize)
	EXPORT_FUNCTION(GetArrayDim)
	EXPORT_FUNCTION(DestroyValue)
	EXPORT_FUNCTION(InitializeValue)
}

FProperty* UFPropertyExporter::GetNativePropertyFromName(UStruct* Struct, const char* PropertyName)
{
	FProperty* Property = FindFProperty<FProperty>(Struct, PropertyName);
	return Property;
}

int32 UFPropertyExporter::GetPropertyOffset(FProperty* Property)
{
	return Property->GetOffset_ForInternal();
}

int32 UFPropertyExporter::GetSize(FProperty* Property)
{
	return Property->GetSize();
}

int32 UFPropertyExporter::GetArrayDim(FProperty* Property)
{
	return Property->ArrayDim;
}

void UFPropertyExporter::DestroyValue(FProperty* Property, void* Value)
{
	Property->DestroyValue(Value);
}

void UFPropertyExporter::InitializeValue(FProperty* Property, void* Value)
{
	Property->InitializeValue(Value);
}

int32 UFPropertyExporter::GetPropertyOffsetFromName(UStruct* InStruct, const char* InPropertyName)
{
	FProperty* FoundProperty = GetNativePropertyFromName(InStruct, InPropertyName);
	if (!FoundProperty)
	{
		return -1;
	}
	
	return GetPropertyOffset(FoundProperty);
}

int32 UFPropertyExporter::GetPropertyArrayDimFromName(UStruct* InStruct, const char* PropertyName)
{
	FProperty* Property = GetNativePropertyFromName(InStruct, PropertyName);
	return GetArrayDim(Property);
}
