
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FQuatExporter
{
    public static delegate* unmanaged<out FQuat, ref FRotator, void> ToQuaternion;
}