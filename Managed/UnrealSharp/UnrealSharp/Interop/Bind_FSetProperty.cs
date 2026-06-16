using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FSetProperty
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetElement;
}