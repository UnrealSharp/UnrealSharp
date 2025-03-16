using System.Runtime.InteropServices;
using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

public struct FTimerDelegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FOnNextTickEvent();
}

[NativeCallbacks]
public static unsafe partial class CSTimerExtensions
{
    public static delegate* unmanaged<IntPtr, void> SetTimerForNextTick;
}