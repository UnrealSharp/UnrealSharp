namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UScriptStructExporter
{
    public static delegate* unmanaged<IntPtr, int> GetNativeStructSize;
}