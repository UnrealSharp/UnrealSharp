using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_ManagedHandle
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> StoreManagedHandle;
    public static delegate* unmanaged<IntPtr, IntPtr> LoadManagedHandle;
    public static delegate* unmanaged<IntPtr, IntPtr, int, void> StoreUnmanagedMemory;
    public static delegate* unmanaged<IntPtr, IntPtr, int, void> LoadUnmanagedMemory;
}