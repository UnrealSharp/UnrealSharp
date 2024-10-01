#include "FMapPropertyExporter.h"

void UFMapPropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetScriptLayout)
}

FScriptMapLayout UFMapPropertyExporter::GetScriptLayout(FMapProperty* MapProperty)
{
	return MapProperty->MapLayout;
}
