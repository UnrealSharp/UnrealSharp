using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FScriptArrayExporter
{
    public static delegate* unmanaged<ref UnmanagedArray, IntPtr> GetData;
    public static delegate* unmanaged<ref UnmanagedArray, NativeBool> IsValidIndex;
    public static delegate* unmanaged<ref UnmanagedArray, int> Num;
    public static delegate* unmanaged<ref UnmanagedArray, void> Destroy;
}