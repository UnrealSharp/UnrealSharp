using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FCSTypeRegistryExporter
{
    public static delegate* unmanaged<char*, char*, void> RegisterClassToFilePath;
}