using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMatrixExporter
{
    public static delegate* unmanaged<out Matrix, ref Rotator, void> FromRotator;
}