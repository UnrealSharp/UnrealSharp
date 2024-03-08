using System.DoubleNumerics;
using UnrealSharp.Core.Attributes;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public unsafe partial class FVectorExporter
{
    public static delegate* unmanaged<out Rotator, System.DoubleNumerics.Vector3> FromRotator;
}