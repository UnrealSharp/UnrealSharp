using System.Runtime.InteropServices;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

public struct FWorldDelegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FWorldCleanupEvent(IntPtr world, NativeBool sessionEnded, NativeBool cleanupResources);
}

[NativeCallbacks]
public static unsafe partial class FWorldDelegatesExporter
{
    public static delegate* unmanaged<IntPtr, out FDelegateHandle, void> BindOnWorldCleanup;
    public static delegate* unmanaged<FDelegateHandle, void> UnbindOnWorldCleanup;
}