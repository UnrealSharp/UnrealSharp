using UnrealSharp.Binds;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FFieldPath
{
    public static delegate* unmanaged<ref FFieldPathUnsafe, NativeBool> IsValid;
    public static delegate* unmanaged<ref FFieldPathUnsafe, NativeBool> IsStale;
    public static delegate* unmanaged<ref FFieldPathUnsafe, ref UnmanagedArray, void> FieldPathToString;
    public static delegate* unmanaged<ref FFieldPathUnsafe, ref FFieldPathUnsafe, NativeBool> FieldPathsEqual;
    public static delegate* unmanaged<ref FFieldPathUnsafe, int> GetFieldPathHashCode;
}