
using UnrealSharp.Binds;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FQuatExporter
{
    public static delegate* unmanaged<out FQuat, FRotator, void> ToQuaternion;
    public static delegate* unmanaged<out FRotator, FQuat, void> ToRotator;
}