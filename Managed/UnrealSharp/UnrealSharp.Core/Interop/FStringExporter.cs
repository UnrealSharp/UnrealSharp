using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public unsafe partial class FStringExporter
{
    public static delegate* unmanaged<UnmanagedArray*, string, void> MarshalToNativeString;
    public static delegate* unmanaged<UnmanagedArray*, char*, int, void> MarshalToNativeStringView;
}
