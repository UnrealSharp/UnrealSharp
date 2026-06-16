using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_TSharedPtr
{
    public static delegate* unmanaged<IntPtr, void> AddSharedReference;
    public static delegate* unmanaged<IntPtr, void> ReleaseSharedReference;
}