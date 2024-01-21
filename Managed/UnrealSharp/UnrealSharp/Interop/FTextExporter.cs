namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FTextExporter
{
    public static delegate* unmanaged<IntPtr, char*> ToString;
    public static delegate* unmanaged<IntPtr, string, void> FromString;
    public static delegate* unmanaged<IntPtr, Name, void> FromName;
    public static delegate* unmanaged<IntPtr, void> CreateEmptyText;
    public static delegate* unmanaged<IntPtr, IntPtr, NativeBool> Compare;
    public static delegate* unmanaged<IntPtr, NativeBool> IsEmpty;
}