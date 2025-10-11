#include "FScriptArrayExporter.h"

void* UFScriptArrayExporter::GetData(FScriptArray* Instance)
{
	return Instance->GetData();
}

bool UFScriptArrayExporter::IsValidIndex(FScriptArray* Instance, int32 i)
{
	return Instance->IsValidIndex(i);
}

void UFScriptArrayExporter::Add(FScriptArray* Instance, int32 Count, int32 NumBytesPerElement, uint32 AlignmentOfElement)
{
	Instance->Add(Count, NumBytesPerElement, AlignmentOfElement);
}

int UFScriptArrayExporter::Num(FScriptArray* Instance)
{
	return Instance->Num();
}

void UFScriptArrayExporter::Destroy(FScriptArray* Instance)
{
	Instance->~FScriptArray();
}
