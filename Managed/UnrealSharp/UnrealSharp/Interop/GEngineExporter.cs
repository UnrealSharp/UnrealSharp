using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class GEngineExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetEngineSubsystem;
}