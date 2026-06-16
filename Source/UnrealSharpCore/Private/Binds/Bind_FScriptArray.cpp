#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FScriptArray)
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
	
	BIND_UNREALSHARP_FUNCTION(GetData)
	BIND_UNREALSHARP_FUNCTION(IsValidIndex)
	BIND_UNREALSHARP_FUNCTION(Add)
	BIND_UNREALSHARP_FUNCTION(Num)
	BIND_UNREALSHARP_FUNCTION(Destroy)
}
