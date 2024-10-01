using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FMatrixExporter
{
    public static delegate* unmanaged<out FMatrix, ref FRotator, void> FromRotator;
}