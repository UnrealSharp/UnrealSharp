using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FVectorExporter
{
    public static delegate* unmanaged<out Rotator, System.DoubleNumerics.Vector3> FromRotator;
}