namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class GEditorExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetEditorSubsystem;
}