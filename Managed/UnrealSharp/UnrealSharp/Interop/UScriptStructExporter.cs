using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UScriptStructExporter
{
    public static delegate* unmanaged<IntPtr, int> GetNativeStructSize;
}