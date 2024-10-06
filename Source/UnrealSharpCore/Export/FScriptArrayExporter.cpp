#include "FScriptArrayExporter.h"

void UFScriptArrayExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetData)
	EXPORT_FUNCTION(IsValidIndex)
	EXPORT_FUNCTION(Num)
	EXPORT_FUNCTION(Destroy)
}

void* UFScriptArrayExporter::GetData(FScriptArray* Instance)
{
	return Instance->GetData();
}

bool UFScriptArrayExporter::IsValidIndex(FScriptArray* Instance, int32 i)
{
	return Instance->IsValidIndex(i);
}

int UFScriptArrayExporter::Num(FScriptArray* Instance)
{
	return Instance->Num();
}

void UFScriptArrayExporter::Destroy(FScriptArray* Instance)
{
	Instance->~FScriptArray();
}
