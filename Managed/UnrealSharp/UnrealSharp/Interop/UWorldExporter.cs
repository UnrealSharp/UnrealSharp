using UnrealSharp.Binds;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UWorldExporter
{
    public static delegate* unmanaged<IntPtr, FName, float, NativeBool, float, FTimerHandle*, void> SetTimer;
    public static delegate* unmanaged<IntPtr, FTimerHandle*, void> InvalidateTimer;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetWorldSubsystem;
    public static delegate* unmanaged<IntPtr, IntPtr> GetNetMode;
}