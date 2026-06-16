using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_UGameInstance
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetGameInstanceSubsystem;
}