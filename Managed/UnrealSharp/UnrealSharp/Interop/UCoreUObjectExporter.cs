using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UCoreUObjectExporter
{
    public static delegate* unmanaged<string, IntPtr> GetNativeClassFromName;
    public static delegate* unmanaged<string, IntPtr> GetNativeStructFromName;
}