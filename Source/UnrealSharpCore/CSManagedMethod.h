#pragma once
#include "CSManagedCallbacksCache.h"
#include "CSManagedGCHandle.h"

struct FGCHandle;

struct UNREALSHARPCORE_API FCSManagedMethod
{
	FCSManagedMethod(const TSharedPtr<FGCHandle>& InMethodHandle)
	{
		MethodHandle = InMethodHandle;
	}

	FCSManagedMethod() = default;

	bool IsValid() const { 	return MethodHandle.IsValid() && !MethodHandle->IsNull(); }
	
	bool Invoke(const FGCHandle& ObjectHandle, uint8* ArgumentBuffer, void* ReturnValue, FString& ExceptionMessage) const
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(FCSManagedMethod::Invoke);
	
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
	
	static FCSManagedMethod Invalid() { return FCSManagedMethod(nullptr); }

private:
	TSharedPtr<FGCHandle> MethodHandle;
};
