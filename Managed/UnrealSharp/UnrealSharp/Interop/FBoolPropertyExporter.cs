using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FBoolPropertyExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, int, bool> GetBitfieldValueFromProperty;
    public static delegate* unmanaged<IntPtr, IntPtr, int, bool, void> SetBitfieldValueForProperty;
}