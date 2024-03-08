using UnrealSharp.Core.Attributes;

namespace UnrealSharp.CoreUObject.Interop;

[NativeCallbacks]
public static unsafe partial class UScriptStructExporter
{
    public static delegate* unmanaged<IntPtr, int> GetNativeStructSize;
}