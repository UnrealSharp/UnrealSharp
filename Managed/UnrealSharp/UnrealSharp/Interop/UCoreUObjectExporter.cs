using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UCoreUObjectExporter
{
    public static delegate* unmanaged<string, string, string, IntPtr> GetNativeClassFromName;
    public static delegate* unmanaged<string, string, string, IntPtr> GetNativeInterfaceFromName;
    public static delegate* unmanaged<string, string, string, IntPtr> GetNativeStructFromName;
}