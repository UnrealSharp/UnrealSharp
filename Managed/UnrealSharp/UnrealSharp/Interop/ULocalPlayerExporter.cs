namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class ULocalPlayerExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetLocalPlayerSubsystem;
}