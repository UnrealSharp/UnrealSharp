#include "CSManagedMethod.h"
#include "CSManagedCallbacksCache.h"
#include "CSManagedGCHandle.h"

bool FCSManagedMethod::Invoke(const FGCHandle& ObjectHandle, uint8* ArgumentBuffer, void* ReturnValue, FString& ExceptionMessage) const
{
	TSharedPtr<FGCHandle> PinnedMethodHandle = MethodHandle.Pin();

#if WITH_EDITOR
	if (GCompilingBlueprint && !PinnedMethodHandle.IsValid())
	{
		// Full reload is in progress. Ignore the call for now.
		return true;
	}
#endif
	
	return FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ObjectHandle.GetHandle(),
		PinnedMethodHandle->GetPointer(),
		ArgumentBuffer,
		ReturnValue,
		&ExceptionMessage) == 0;
}
