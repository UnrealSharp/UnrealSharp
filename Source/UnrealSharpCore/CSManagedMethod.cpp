#include "CSManagedMethod.h"
#include "CSManagedCallbacksCache.h"
#include "CSManagedGCHandle.h"

bool FCSManagedMethod::IsValid() const
{
	return MethodHandle.IsValid() && !MethodHandle->IsNull();
}

bool FCSManagedMethod::Invoke(const FGCHandle& ObjectHandle, uint8* ArgumentBuffer, void* ReturnValue, FString& ExceptionMessage) const
{
#if WITH_EDITOR
	if (GCompilingBlueprint && (!MethodHandle.IsValid() || !MethodHandle->IsNull()))
	{
		// Full reload is in progress. Ignore the call for now.
		return true;
	}
#endif
	
	return FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ObjectHandle.GetHandle(),
		MethodHandle->GetPointer(),
		ArgumentBuffer,
		ReturnValue,
		&ExceptionMessage) == 0;
}
