using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UGameInstanceExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetGameInstanceSubsystem;
}