using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FFieldPathExporter
{
    public static delegate* unmanaged<ref FFieldPathUnsafe, NativeBool> IsValid;
    public static delegate* unmanaged<ref FFieldPathUnsafe, NativeBool> IsStale;
    public static delegate* unmanaged<ref FFieldPathUnsafe, ref UnmanagedArray, void> FieldPathToString;
    public static delegate* unmanaged<ref FFieldPathUnsafe, ref FFieldPathUnsafe, NativeBool> FieldPathsEqual;
    public static delegate* unmanaged<ref FFieldPathUnsafe, int> GetFieldPathHashCode;
}