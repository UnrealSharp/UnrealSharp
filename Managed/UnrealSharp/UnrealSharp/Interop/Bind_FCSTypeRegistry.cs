using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FCSTypeRegistry
{
    public static delegate* unmanaged<char*, char*, void> RegisterClassToFilePath;
}