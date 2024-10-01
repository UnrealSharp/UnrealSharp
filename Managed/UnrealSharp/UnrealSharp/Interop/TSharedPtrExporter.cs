using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class TSharedPtrExporter
{
    public static delegate* unmanaged<IntPtr, void> AddSharedReference;
    public static delegate* unmanaged<IntPtr, void> ReleaseSharedReference;
}