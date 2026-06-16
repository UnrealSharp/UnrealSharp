using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FString
{
    public static delegate* unmanaged<UnmanagedArray*, string, void> MarshalToNativeString;
    public static delegate* unmanaged<UnmanagedArray*, char*, int, void> MarshalToNativeStringView;
}
