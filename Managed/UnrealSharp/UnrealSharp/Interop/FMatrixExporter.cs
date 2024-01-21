namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMatrixExporter
{
    public static delegate* unmanaged<out System.DoubleNumerics.Matrix4x4, ref Rotator, void> FromRotator;
}