using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class GEditorExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetEditorSubsystem;
}