using UnrealSharp.Binds;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FRotatorExporter
{
    public static delegate* unmanaged<out FRotator, ref FMatrix, void> FromMatrix;
}