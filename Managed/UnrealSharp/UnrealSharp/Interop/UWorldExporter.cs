using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UWorldExporter
{
    public static delegate* unmanaged<IntPtr, FTransform*, IntPtr, ref FActorSpawnParameters, IntPtr> SpawnActor;
    public static delegate* unmanaged<IntPtr, FName, float, NativeBool, float, FTimerHandle*, void> SetTimer;
    public static delegate* unmanaged<IntPtr, FTimerHandle*, void> InvalidateTimer;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetWorldSubsystem;
}