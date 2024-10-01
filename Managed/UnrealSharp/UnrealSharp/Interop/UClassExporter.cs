using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UClassExporter
{
    public static delegate* unmanaged<IntPtr, string, IntPtr> GetNativeFunctionFromClassAndName;
    public static delegate* unmanaged<IntPtr, string, IntPtr> GetNativeFunctionFromInstanceAndName;
    public static delegate* unmanaged<string, IntPtr> GetDefaultFromName;
    public static delegate* unmanaged<IntPtr, IntPtr> GetDefaultFromInstance;
}