using System.Runtime.InteropServices;
using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

#if WITH_EDITOR

[NativeCallbacks]
public static unsafe partial class Bind_ScopedTransaction
{
    public static delegate* unmanaged<string, IntPtr> Create;
    public static delegate* unmanaged<IntPtr, void> Destroy;
    public static delegate* unmanaged<IntPtr, void> Cancel;
}

#endif
