using System.Runtime.InteropServices;
using UnrealSharp.Binds;

namespace UnrealSharp;

[NativeCallbacks]
public static unsafe partial class IRefCountedObjectExporter
{
    public static delegate* unmanaged<IntPtr, uint> AddRef;
    public static delegate* unmanaged<IntPtr, uint> Release;
    public static delegate* unmanaged<IntPtr, uint> GetRefCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct FSharedPtr
{
    private IntPtr ReferenceController;
    
    public void AddRef()
    {
        if (Valid)
        {
            IRefCountedObjectExporter.CallAddRef(ReferenceController);
        }
    }
    
    public void Release()
    {
        if (Valid)
        {
            IRefCountedObjectExporter.CallRelease(ReferenceController);
        }
    }
    
    public uint RefCount => IRefCountedObjectExporter.CallGetRefCount(ReferenceController);
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