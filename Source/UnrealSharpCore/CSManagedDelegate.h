#pragma once
#include "CSManagedGCHandle.h"

struct UNREALSHARPCORE_API FCSManagedDelegate
{
	FCSManagedDelegate(const FGCHandle& ManagedDelegate)
		: CallbackHandle(ManagedDelegate)
	{
		
	}

	FCSManagedDelegate() : CallbackHandle()
	{
		
	}
	
	void Invoke(UObject* WorldContextObject = nullptr, bool bDispose = true);
	void Dispose();

private:
	FGCHandle CallbackHandle;
};
