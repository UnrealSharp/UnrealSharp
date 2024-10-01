using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal unsafe partial class FStringExporter
{
    public static delegate* unmanaged<IntPtr, char*, void> MarshalToNativeString;
}