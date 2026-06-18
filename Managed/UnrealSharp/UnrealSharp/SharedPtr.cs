using System.Runtime.InteropServices;
using UnrealSharp.Binds;

namespace UnrealSharp;

[NativeCallbacks]
public static unsafe partial class Bind_IRefCountedObject
{
    public static delegate* unmanaged<IntPtr, void> AddRef;
    public static delegate* unmanaged<IntPtr, void> Release;
}

[StructLayout(LayoutKind.Sequential)]
public struct FSharedPtr
{
    private IntPtr ReferenceController;
    
    public void AddRef()
    {
        if (Valid)
        {
            Bind_IRefCountedObject.CallAddRef(ReferenceController);
        }
    }
    
    public void Release()
    {
        if (Valid)
        {
            Bind_IRefCountedObject.CallRelease(ReferenceController);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is FSharedPtr ptr && ReferenceController == ptr.ReferenceController;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReferenceController);
    }

    public bool Valid => ReferenceController != IntPtr.Zero;

    public static bool operator ==(FSharedPtr a, FSharedPtr b)
    {
        return a.ReferenceController == b.ReferenceController;
    }

    public static bool operator !=(FSharedPtr a, FSharedPtr b)
    {
        return !(a == b);
    }
}