using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FCSManagedCallbacks
{
    public static delegate* unmanaged<IntPtr> GetManagedCallbacks;
}