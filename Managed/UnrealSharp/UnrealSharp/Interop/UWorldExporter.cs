namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UWorldExporter
{
    public static delegate* unmanaged<IntPtr, Transform, IntPtr, ActorSpawnParameters, IntPtr> SpawnActor;
    public static delegate* unmanaged<IntPtr, string, float, NativeBool, TimerHandle*, void> SetTimer;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetWorldSubsystem;
}