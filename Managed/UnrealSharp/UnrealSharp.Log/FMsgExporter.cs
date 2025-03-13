using UnrealSharp.Binds;

namespace UnrealSharp.Log;

[NativeCallbacks]
public static unsafe partial class FMsgExporter
{
    public static delegate* unmanaged<char*, ELogVerbosity, char*, void> Log;
}