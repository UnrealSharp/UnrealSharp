using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UDataTableExporter
{
    public static delegate* unmanaged<IntPtr, FName, IntPtr> GetRow;
}