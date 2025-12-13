namespace UnrealSharp.Binds;

public static class NativeBinds
{
    private static unsafe delegate* unmanaged[Cdecl]<char*, char*, int, IntPtr> _getBoundFunction = null;

    public static unsafe void Initialize(IntPtr bindsCallbacks)
    {
        if (_getBoundFunction != null)
        {
            throw new Exception("NativeBinds.Initialize called twice");
        }

        _getBoundFunction = (delegate* unmanaged[Cdecl]<char*, char*, int, IntPtr>)bindsCallbacks;
    }

    public static unsafe IntPtr TryGetBoundFunction(string outerName, string functionName, int functionSize)
    {
        if (_getBoundFunction == null)
        {
            throw new Exception("NativeBinds not initialized");
        }

        IntPtr functionPtr;
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