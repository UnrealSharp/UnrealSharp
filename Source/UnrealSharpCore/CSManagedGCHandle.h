#pragma once

enum class GCHandleType : char
{
	Null,
	StrongHandle,
	WeakHandle,
	PinnedHandle,
};

struct GCHandleIntPtr
{
	bool operator == (const GCHandleIntPtr& Other) const
	{
		return IntPtr == Other.IntPtr;
	}

	bool operator != (const GCHandleIntPtr& Other) const
	{
		return IntPtr != Other.IntPtr;
	}
	
	// Pointer to the managed object in C#
	uint8* IntPtr = nullptr;
};

static_assert(sizeof(GCHandleIntPtr) == sizeof(void *));

struct FGCHandle
{
	GCHandleIntPtr Handle;
	GCHandleType Type = GCHandleType::Null;

	bool IsNull() const { return !Handle.IntPtr; }
	bool IsWeakPointer() const { return Type == GCHandleType::WeakHandle; }
	const GCHandleIntPtr& GetHandle() const { return Handle; }
	void* GetIntPtr() const { return Handle.IntPtr; };
	
	void Dispose();

	void operator = (const FGCHandle& Other)
	{
		Handle = Other.Handle;
		Type = Other.Type;
	}

	FGCHandle()
	{
		
	}

	FGCHandle(GCHandleIntPtr InHandle) : Handle(InHandle)
	{
		Type = GCHandleType::Null;
	}
};

