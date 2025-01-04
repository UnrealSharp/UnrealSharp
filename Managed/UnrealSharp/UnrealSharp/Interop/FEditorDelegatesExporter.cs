using System.Runtime.InteropServices;

namespace UnrealSharp.Interop;

public struct FEditorDelegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FOnPIEEvent(NativeBool sessionEnded);
}

[NativeCallbacks]
public static unsafe partial class FEditorDelegatesExporter
{
    public static delegate* unmanaged<IntPtr, out FDelegateHandle, void> BindStartPIE;
    public static delegate* unmanaged<IntPtr, out FDelegateHandle, void> BindEndPIE;
    public static delegate* unmanaged<FDelegateHandle, void> UnbindStartPIE;
    public static delegate* unmanaged<FDelegateHandle, void> UnbindEndPIE;
}