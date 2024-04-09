namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UWorldExporter
{
    public static delegate* unmanaged<IntPtr, Transform*, IntPtr, ref ActorSpawnParameters, IntPtr> SpawnActor;
    public static delegate* unmanaged<IntPtr, string, float, NativeBool, TimerHandle*, void> SetTimer;
    public static delegate* unmanaged<IntPtr, TimerHandle*, void> InvalidateTimer;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetWorldSubsystem;
}