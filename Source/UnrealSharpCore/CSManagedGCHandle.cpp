#include "CSManagedGCHandle.h"
#include "CSManagedCallbacksCache.h"

void FGCHandle::Dispose(FGCHandleIntPtr AssemblyHandle)
{
	if (!Handle.IntPtr || Type == GCHandleType::Null)
	{
		return;
	}

	FCSManagedCallbacks::ManagedCallbacks.Dispose(Handle, AssemblyHandle);
	
	Handle.IntPtr = nullptr;
	Type = GCHandleType::Null;
}
