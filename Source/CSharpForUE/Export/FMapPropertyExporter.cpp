#include "FMapPropertyExporter.h"

void UFMapPropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetKeyProperty)
	EXPORT_FUNCTION(GetValueProperty)
	EXPORT_FUNCTION(GetScriptLayout)
}

void* UFMapPropertyExporter::GetKeyProperty(FMapProperty* MapProperty)
{
	return MapProperty->KeyProp;
}

void* UFMapPropertyExporter::GetValueProperty(FMapProperty* MapProperty)
{
	return MapProperty->ValueProp;
}

FScriptMapLayout UFMapPropertyExporter::GetScriptLayout(FMapProperty* MapProperty)
{
	return MapProperty->MapLayout;
}
