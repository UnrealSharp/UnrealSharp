using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptArrayExporter
{
    public static delegate* unmanaged<UnmanagedArray*, IntPtr> GetData;
    public static delegate* unmanaged<UnmanagedArray*, int, NativeBool> IsValidIndex;
    public static delegate* unmanaged<UnmanagedArray*, int> Num;
    public static delegate* unmanaged<UnmanagedArray*, void> Destroy;
}