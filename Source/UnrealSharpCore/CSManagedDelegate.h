#pragma once
#include "CSManagedGCHandle.h"

struct UNREALSHARPCORE_API FCSManagedDelegate
{
	FCSManagedDelegate(const FGCHandle& ManagedDelegate)
		: CallbackHandle(ManagedDelegate)
	{
		
	}

	FCSManagedDelegate()
	{
		
	}
	
	void Invoke(UObject* WorldContextObject = nullptr, bool bDispose = true);
	void Dispose() { CallbackHandle.Dispose(); }

private:
	FGCHandle CallbackHandle;
};
