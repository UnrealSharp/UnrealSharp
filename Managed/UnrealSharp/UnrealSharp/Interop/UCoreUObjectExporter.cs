namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UCoreUObjectExporter
{
    public static delegate* unmanaged<string, IntPtr> GetNativeClassFromName;
    public static delegate* unmanaged<string, IntPtr> GetNativeStructFromName;
}