using System.Runtime.InteropServices;

namespace UnrealSharp.Binds;

[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
internal delegate IntPtr GetBoundFunction(string outerName, string functionName, int functionSize);

public static class NativeBinds
{
    private static GetBoundFunction? _getBoundFunction;

    public static void InitializeNativeBinds(IntPtr bindsCallbacks)
    {
        if (_getBoundFunction != null)
        {
            throw new Exception("NativeBinds.InitializeNativeBinds called twice");
        }
        
        _getBoundFunction = Marshal.GetDelegateForFunctionPointer<GetBoundFunction>(bindsCallbacks);
    }
    
    public static IntPtr TryGetBoundFunction(string outerName, string functionName, int functionSize)
    {
        if (_getBoundFunction == null)
        {
            throw new Exception("NativeBinds not initialized");
        }
        
        IntPtr functionPtr = _getBoundFunction(outerName, functionName, functionSize);
                
        if (functionPtr == IntPtr.Zero)
        {
            throw new Exception($"Failed to find bound function {functionName} in {outerName}");
        }
                
        return functionPtr;
    }
}