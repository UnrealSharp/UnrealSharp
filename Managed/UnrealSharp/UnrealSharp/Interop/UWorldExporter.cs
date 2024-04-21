using UnrealSharp.Engine;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UWorldExporter
{
    public static delegate* unmanaged<IntPtr, CoreUObject.Transform*, IntPtr, ref ActorSpawnParameters, IntPtr> SpawnActor;
    public static delegate* unmanaged<IntPtr, Name, float, NativeBool, float, TimerHandle*, void> SetTimer;
    public static delegate* unmanaged<IntPtr, TimerHandle*, void> InvalidateTimer;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetWorldSubsystem;
}