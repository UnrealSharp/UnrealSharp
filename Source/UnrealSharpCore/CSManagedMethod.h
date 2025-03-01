#pragma once

struct FGCHandle;

struct UNREALSHARPCORE_API FCSManagedMethod
{
	FCSManagedMethod(const TSharedPtr<FGCHandle>& InMethodHandle)
	{
		MethodHandle = InMethodHandle;
	}

	FCSManagedMethod() = default;

	bool IsValid() const;
	bool Invoke(const FGCHandle& ObjectHandle, uint8* ArgumentBuffer, void* ReturnValue, FString& ExceptionMessage) const;
	
	static FCSManagedMethod Invalid() { return FCSManagedMethod(nullptr); }

private:
	TSharedPtr<FGCHandle> MethodHandle;
};
