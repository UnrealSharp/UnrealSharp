using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal unsafe partial class FVectorExporter
{
    public static delegate* unmanaged<out FRotator, FVector> FromRotator;
}