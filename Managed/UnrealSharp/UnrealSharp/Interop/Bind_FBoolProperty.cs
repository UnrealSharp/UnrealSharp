using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FBoolProperty
{
    public static delegate* unmanaged<IntPtr, IntPtr, int, NativeBool> GetBitfieldValueFromProperty;
    public static delegate* unmanaged<IntPtr, IntPtr, int, NativeBool, void> SetBitfieldValueForProperty;
}