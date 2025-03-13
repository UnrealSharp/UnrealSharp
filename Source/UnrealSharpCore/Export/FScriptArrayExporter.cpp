#include "FScriptArrayExporter.h"

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
