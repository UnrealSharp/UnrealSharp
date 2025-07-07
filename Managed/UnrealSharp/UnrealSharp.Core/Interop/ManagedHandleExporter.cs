using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public unsafe partial class ManagedHandleExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> StoreManagedHandle;
    public static delegate* unmanaged<IntPtr, IntPtr> LoadManagedHandle;
}