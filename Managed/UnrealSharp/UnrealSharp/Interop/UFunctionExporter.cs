using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UFunctionExporter
{
    public static delegate* unmanaged<IntPtr, UInt16> GetNativeFunctionParamsSize;
}