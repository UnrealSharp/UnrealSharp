using UnrealSharp.Binds;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FRotator
{
    public static delegate* unmanaged<ref FRotator, FMatrix, void> FromMatrix;
}