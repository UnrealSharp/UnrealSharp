using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_UStruct
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> InitializeStruct;
}