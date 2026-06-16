using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FBoolProperty
{
    public static delegate* unmanaged<IntPtr, IntPtr, int, bool> GetBitfieldValueFromProperty;
    public static delegate* unmanaged<IntPtr, IntPtr, int, bool, void> SetBitfieldValueForProperty;
}