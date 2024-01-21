namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UClassExporter
{
    public static delegate* unmanaged<IntPtr, string, IntPtr> GetNativeFunctionFromClassAndName;
    public static delegate* unmanaged<IntPtr, string, IntPtr> GetNativeFunctionFromInstanceAndName;
    public static delegate* unmanaged<string, IntPtr> GetDefaultFromString;
    public static delegate* unmanaged<IntPtr, IntPtr> GetDefaultFromInstance;
}