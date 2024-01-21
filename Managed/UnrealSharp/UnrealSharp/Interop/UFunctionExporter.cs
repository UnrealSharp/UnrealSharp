namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UFunctionExporter
{
    public static delegate* unmanaged<IntPtr, UInt16> GetNativeFunctionParamsSize;
}