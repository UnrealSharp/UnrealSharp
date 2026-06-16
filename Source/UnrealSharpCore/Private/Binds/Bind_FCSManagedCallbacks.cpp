#include "CSBindsRegistry.h"
#include "CSManagedCallbacksCache.h"

DECLARE_UNREALSHARP_BINDER(Bind_FCSManagedCallbacks)
{
	FCSManagedCallbacks* GetManagedCallbacks()
	{
		return &::GetManagedCallbacks();
	}
	
	BIND_UNREALSHARP_FUNCTION(GetManagedCallbacks)
}
