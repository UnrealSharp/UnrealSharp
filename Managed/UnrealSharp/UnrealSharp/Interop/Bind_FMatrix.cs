using UnrealSharp.Binds;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FMatrix
{
    public static delegate* unmanaged<out FMatrix, FRotator, void> FromRotator;
}