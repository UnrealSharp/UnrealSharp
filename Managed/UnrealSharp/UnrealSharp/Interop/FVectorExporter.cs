using UnrealSharp.Binds;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FVectorExporter
{
    public static delegate* unmanaged<FRotator, FVector> FromRotator;
}