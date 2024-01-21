namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class TSharedPtrExporter
{
    public static delegate* unmanaged<IntPtr, void> AddSharedReference;
    public static delegate* unmanaged<IntPtr, void> ReleaseSharedReference;
}