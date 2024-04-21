namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptArrayExporter
{
    public static delegate* unmanaged<ref UnmanagedArray, IntPtr> GetData;
    public static delegate* unmanaged<ref UnmanagedArray, NativeBool> IsValidIndex;
    public static delegate* unmanaged<ref UnmanagedArray, int> Num;
    public static delegate* unmanaged<ref UnmanagedArray, void> Destroy;
}