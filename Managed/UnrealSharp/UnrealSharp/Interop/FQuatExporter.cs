using UnrealSharp.CoreUObject;
namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FQuatExporter
{
    public static delegate* unmanaged<out Quat, ref Rotator, void> ToQuaternion;
}