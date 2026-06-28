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
public record struct FSharedPtr
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

    public override int GetHashCode()
    {
        return HashCode.Combine(ReferenceController);
    }

    public bool Valid => ReferenceController != IntPtr.Zero;
}