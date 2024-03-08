using UnrealSharp.Core.Attributes;

namespace UnrealSharp.CoreUObject.Interop;

[NativeCallbacks]
public static unsafe partial class UFunctionExporter
{
    public static delegate* unmanaged<IntPtr, UInt16> GetNativeFunctionParamsSize;
}