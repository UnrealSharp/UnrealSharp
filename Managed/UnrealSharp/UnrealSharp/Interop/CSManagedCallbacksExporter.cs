using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FCSManagedCallbacksExporter
{
    public static delegate* unmanaged<IntPtr> GetManagedCallbacks;
}