using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class ULocalPlayerExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetLocalPlayerSubsystem;
}