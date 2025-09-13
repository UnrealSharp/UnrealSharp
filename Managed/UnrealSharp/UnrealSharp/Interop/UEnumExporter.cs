using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UEnumExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetManagedEnumType;
}