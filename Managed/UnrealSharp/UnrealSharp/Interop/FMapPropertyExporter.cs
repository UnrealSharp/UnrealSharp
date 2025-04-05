using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMapPropertyExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetKey;
    public static delegate* unmanaged<IntPtr, IntPtr> GetValue;
}