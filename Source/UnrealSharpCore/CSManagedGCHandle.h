#pragma once

enum class GCHandleType : char
{
	Null,
	StrongHandle,
	WeakHandle,
	PinnedHandle,
};

struct FGCHandleIntPtr
{
	bool operator == (const FGCHandleIntPtr& Other) const
	{
		return IntPtr == Other.IntPtr;
	}

	bool operator != (const FGCHandleIntPtr& Other) const
	{
		return IntPtr != Other.IntPtr;
	}
	
	// Pointer to the managed object in C#
	uint8* IntPtr = nullptr;
};

static_assert(sizeof(FGCHandleIntPtr) == sizeof(void *));

struct FGCHandle
{
	FGCHandleIntPtr Handle;
	GCHandleType Type = GCHandleType::Null;

	bool IsNull() const { return !Handle.IntPtr; }
	bool IsWeakPointer() const { return Type == GCHandleType::WeakHandle; }
	
	const FGCHandleIntPtr& GetHandle() const { return Handle; }
	uint8* GetPointer() const { return Handle.IntPtr; };
	
	void Dispose(FGCHandleIntPtr AssemblyHandle = FGCHandleIntPtr());

	void operator = (const FGCHandle& Other)
	{
		Handle = Other.Handle;
		Type = Other.Type;
	}

	FGCHandle(){}
	FGCHandle(const FGCHandleIntPtr InHandle, const GCHandleType InType) : Handle(InHandle), Type(InType) {}

	FGCHandle(uint8* InHandle, const GCHandleType InType) : Type(InType)
	{
		Handle.IntPtr = InHandle;
	}

	FGCHandle(const FGCHandleIntPtr InHandle) : Handle(InHandle)
	{
		Type = GCHandleType::Null;
	}
};

