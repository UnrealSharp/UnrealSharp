using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMatrixExporter
{
    public static delegate* unmanaged<out FMatrix, ref FRotator, void> FromRotator;
}