using System.DoubleNumerics;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FRotatorExporter
{
    public static delegate* unmanaged<out Rotator, ref System.DoubleNumerics.Quaternion, void> FromQuat;
    public static delegate* unmanaged<out Rotator, ref System.DoubleNumerics.Matrix4x4, void> FromMatrix;
}