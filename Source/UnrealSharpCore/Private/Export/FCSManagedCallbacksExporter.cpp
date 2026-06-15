#include "CSBindsManager.h"
#include "CSManagedCallbacksCache.h"

DECLARE_UNREALSHARP_EXPORTER(FCSManagedCallbacksExporter)
{
	FCSManagedCallbacks* GetManagedCallbacks()
	{
		return &::GetManagedCallbacks();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetManagedCallbacks)
}
