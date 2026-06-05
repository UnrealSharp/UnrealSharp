#pragma once

#include "CSManagedCallbacksCache.h"
#include "CSManagedGCHandle.generated.h"

struct FGCHandleIntPtr
{
    bool operator==(const FGCHandleIntPtr& Other) const = default;
    uint8* ManagedHandlePtr = nullptr;
};

static_assert(sizeof(FGCHandleIntPtr) == sizeof(void*));

struct FGCHandle
{
    FGCHandle() = default;
    FGCHandle(FGCHandleIntPtr InHandle) : Handle(InHandle) {}
    FGCHandle(uint8* InHandle) : Handle{InHandle} {}

    static FGCHandle InvalidHandle() { return FGCHandle(nullptr); }

    FGCHandleIntPtr GetHandle() const { return Handle; }
    uint8* GetPointer() const { return Handle.ManagedHandlePtr; }
    bool IsNull() const { return Handle.ManagedHandlePtr == nullptr; }

    void Dispose(FGCHandleIntPtr AssemblyHandle = {})
    {
        TRACE_CPUPROFILER_EVENT_SCOPE(FGCHandle::Dispose);

        if (IsNull())
        {
            return;
        }

        GetManagedCallbacks().Dispose(Handle, AssemblyHandle);
        Invalidate();
    }

    void Invalidate() { Handle.ManagedHandlePtr = nullptr; }

    operator void*() const { return Handle.ManagedHandlePtr; }
    
private:
    FGCHandleIntPtr Handle;
};

struct FScopedGCHandle
{
    explicit FScopedGCHandle(FGCHandleIntPtr InHandle) : Handle(InHandle) {}

    FScopedGCHandle(const FScopedGCHandle&) = delete;
    FScopedGCHandle& operator=(const FScopedGCHandle&) = delete;

    ~FScopedGCHandle()
    {
        if (Handle.ManagedHandlePtr != nullptr)
        {
            GetManagedCallbacks().FreeHandle(Handle);
        }
    }
    
    FGCHandleIntPtr GetHandle() const { return Handle; }
    
private:
    FGCHandleIntPtr Handle;
};

USTRUCT()
struct FSharedGCHandle
{
    GENERATED_BODY()

    FSharedGCHandle() = default;
    explicit FSharedGCHandle(FGCHandleIntPtr InHandle) : Handle(MakeShared<FScopedGCHandle>(InHandle)) {}

    FGCHandleIntPtr GetHandle() const
    {
        return Handle ? Handle->GetHandle() : FGCHandleIntPtr{};
    }

private:
    TSharedPtr<FScopedGCHandle> Handle;
};