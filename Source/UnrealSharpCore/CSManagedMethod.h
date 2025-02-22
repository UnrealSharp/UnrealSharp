#pragma once

struct FGCHandle;

struct UNREALSHARPCORE_API FCSManagedMethod
{
	FCSManagedMethod(const TWeakPtr<FGCHandle>& InMethodHandle)
	{
		MethodHandle = InMethodHandle;
	}

	FCSManagedMethod() = default;

	bool IsValid() const { return MethodHandle.IsValid(); }
	bool Invoke(const FGCHandle& ObjectHandle, uint8* ArgumentBuffer, void* ReturnValue, FString& ExceptionMessage) const;
	
	static FCSManagedMethod Invalid() { return FCSManagedMethod(nullptr); }

private:
	TWeakPtr<FGCHandle> MethodHandle;
};
