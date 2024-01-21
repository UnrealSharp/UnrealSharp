namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UGameInstanceExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetGameInstanceSubsystem;
}