namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FQuatExporter
{
    public static delegate* unmanaged<out System.DoubleNumerics.Quaternion, ref Rotator, void> ToQuaternion;
}