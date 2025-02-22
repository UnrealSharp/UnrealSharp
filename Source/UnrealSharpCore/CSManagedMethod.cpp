#include "CSManagedMethod.h"
#include "CSManagedCallbacksCache.h"
#include "CSManagedGCHandle.h"

bool FCSManagedMethod::Invoke(const FGCHandle& ObjectHandle, uint8* ArgumentBuffer, void* ReturnValue, FString& ExceptionMessage) const
{
	TSharedPtr<FGCHandle> PinnedMethodHandle = MethodHandle.Pin();
	return FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ObjectHandle.GetHandle(),
		PinnedMethodHandle->GetPointer(),
		ArgumentBuffer,
		ReturnValue,
		&ExceptionMessage) == 0;
}
