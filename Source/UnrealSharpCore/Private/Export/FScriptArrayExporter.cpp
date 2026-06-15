#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FScriptArrayExporter)
{
	void* GetData(FScriptArray* Instance)
	{
		return Instance->GetData();
	}

	bool IsValidIndex(FScriptArray* Instance, int32 i)
	{
		return Instance->IsValidIndex(i);
	}

	void Add(FScriptArray* Instance, int32 Count, int32 NumBytesPerElement, uint32 AlignmentOfElement)
	{
		Instance->Add(Count, NumBytesPerElement, AlignmentOfElement);
	}

	int Num(FScriptArray* Instance)
	{
		return Instance->Num();
	}

	void Destroy(FScriptArray* Instance)
	{
		Instance->~FScriptArray();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetData)
	EXPORT_UNREALSHARP_FUNCTION(IsValidIndex)
	EXPORT_UNREALSHARP_FUNCTION(Add)
	EXPORT_UNREALSHARP_FUNCTION(Num)
	EXPORT_UNREALSHARP_FUNCTION(Destroy)
}
