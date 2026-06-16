using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FMapProperty
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetKey;
    public static delegate* unmanaged<IntPtr, IntPtr> GetValue;
}