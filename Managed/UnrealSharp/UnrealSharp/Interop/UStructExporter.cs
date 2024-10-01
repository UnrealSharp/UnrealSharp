using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UStructExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> InitializeStruct;
}