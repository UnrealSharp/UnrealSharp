using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FRotatorExporter
{
    public static delegate* unmanaged<out FRotator, ref FMatrix, void> FromMatrix;
}