
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FQuatExporter
{
    public static delegate* unmanaged<out FQuat, ref FRotator, void> ToQuaternion;
    public static delegate* unmanaged<out FRotator, ref FQuat, void> ToRotator;
}