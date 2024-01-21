namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class GEngineExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetEngineSubsystem;
}