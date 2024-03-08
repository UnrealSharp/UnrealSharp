using UnrealSharp.Core.Attributes;

namespace UnrealSharp.CoreUObject.Interop;

[NativeCallbacks]
public static unsafe partial class UCoreUObjectExporter
{
    public static delegate* unmanaged<string, IntPtr> GetNativeClassFromName;
    public static delegate* unmanaged<string, IntPtr> GetNativeStructFromName;
}