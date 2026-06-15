#include "CSBindsManager.h"
#include "CSManagedGCHandle.h"
#include "CSUnmanagedDataStore.h"

DECLARE_UNREALSHARP_EXPORTER(ManagedHandleExporter)
{
    void StoreManagedHandle(const FGCHandleIntPtr Handle, FSharedGCHandle& Destination)
    {
        Destination = FSharedGCHandle(Handle);
    }

    FGCHandleIntPtr LoadManagedHandle(const FSharedGCHandle& Source)
    {
        return Source.GetHandle();
    }

    void StoreUnmanagedMemory(const void* Source, FUnmanagedDataStore& Destination, const int32 Size)
    {
        check(Size > 0)
        Destination.CopyDataIn(Source, Size);
    }

    void LoadUnmanagedMemory(const FUnmanagedDataStore& Source, void* Destination, const int32 Size)
    {
        check(Size > 0)
        Source.CopyDataOut(Destination, Size);
    }
    
    EXPORT_UNREALSHARP_FUNCTION(StoreManagedHandle)
    EXPORT_UNREALSHARP_FUNCTION(LoadManagedHandle)
    EXPORT_UNREALSHARP_FUNCTION(StoreUnmanagedMemory)
    EXPORT_UNREALSHARP_FUNCTION(LoadUnmanagedMemory)
}
