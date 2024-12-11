using UnrealSharp.Logging;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMsgExporter
{
    public static delegate* unmanaged<FName, ELogVerbosity, char*, void> Log;
}