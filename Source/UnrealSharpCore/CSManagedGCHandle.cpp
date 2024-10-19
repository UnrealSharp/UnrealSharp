#include "CSManagedGCHandle.h"
#include "CSManagedCallbacksCache.h"

void FGCHandle::Dispose()
{
	if (!Handle.IntPtr)
	{
		return;
	}

	FCSManagedCallbacks::ManagedCallbacks.Dispose(Handle);
	
	Handle.IntPtr = nullptr;
	Type = GCHandleType::Null;
}
