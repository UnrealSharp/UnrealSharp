using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FUtf8String
{
    public static delegate* unmanaged<UnmanagedArray*, byte*, int, void> Assign;
    public static delegate* unmanaged<UnmanagedArray*, void> Initialize;
    public static delegate* unmanaged<UnmanagedArray*, void> Destroy;
}
