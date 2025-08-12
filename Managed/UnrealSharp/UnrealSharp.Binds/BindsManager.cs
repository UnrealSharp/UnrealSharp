namespace UnrealSharp.Binds;

public static class NativeBinds
{
    private unsafe static delegate* unmanaged[Cdecl]<char*, char*, int, IntPtr> _getBoundFunction = null;

    public unsafe static void InitializeNativeBinds(IntPtr bindsCallbacks)
    {
        if (_getBoundFunction != null)
        {
            throw new Exception("NativeBinds.InitializeNativeBinds called twice");
        }

        _getBoundFunction = (delegate* unmanaged[Cdecl]<char*, char*, int, nint>)bindsCallbacks;
    }

    public unsafe static IntPtr TryGetBoundFunction(string outerName, string functionName, int functionSize)
    {
        if (_getBoundFunction == null)
        {
            throw new Exception("NativeBinds not initialized");
        }

        IntPtr functionPtr = IntPtr.Zero;
        fixed (char* outerNamePtr = outerName)
        fixed (char* functionNamePtr = functionName)
        {
            functionPtr = _getBoundFunction(outerNamePtr, functionNamePtr, functionSize);
        }

        if (functionPtr == IntPtr.Zero)
        {
            throw new Exception($"Failed to find bound function {functionName} in {outerName}");
        }

        return functionPtr;
    }
}