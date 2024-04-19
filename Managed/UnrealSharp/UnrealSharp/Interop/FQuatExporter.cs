using UnrealSharp.CoreUObject;
namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FQuatExporter
{
    public static delegate* unmanaged<out Quat, ref Rotator, void> ToQuaternion;
}