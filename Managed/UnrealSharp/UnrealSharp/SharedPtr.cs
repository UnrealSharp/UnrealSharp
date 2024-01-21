using System.Runtime.InteropServices;
using System.Diagnostics;
using UnrealSharp.Interop;

namespace UnrealSharp;

class SharedPtrTheadSafe : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    struct FMarshaledSharedPtr
    {
        public IntPtr ObjectPtr;
        public IntPtr ReferenceController;
    };

    public bool OwnsAReference
    { get; private set; }

    public IntPtr NativeInstance { get; private set; }

    public unsafe IntPtr ObjectPtr
    {
        get
        {
            Debug.Assert(NativeInstance != IntPtr.Zero, "Can't access a null SharedPtr.");
            return ((FMarshaledSharedPtr*)NativeInstance)->ObjectPtr;
        }
        private set
        {
            Debug.Assert(NativeInstance != IntPtr.Zero, "Can't access a null SharedPtr.");
            ((FMarshaledSharedPtr*)NativeInstance)->ObjectPtr = value;
        }
    }
        
    public unsafe IntPtr ReferenceController 
    { 
        get
        {
            Debug.Assert(NativeInstance != IntPtr.Zero, "Can't access a null SharedPtr.");
            return ((FMarshaledSharedPtr*)NativeInstance)->ReferenceController;
        }
        private set
        {
            Debug.Assert(NativeInstance != IntPtr.Zero, "Can't access a null SharedPtr.");
            ((FMarshaledSharedPtr*)NativeInstance)->ReferenceController = value;
        }
    }

    public int ReferenceCount
    {
        get
        {
            Debug.Assert(NativeInstance != IntPtr.Zero, "Can't access a null SharedPtr.");
            return Marshal.ReadInt32(ReferenceController);
        }
    }

    private SharedPtrTheadSafe()
    {
        OwnsAReference = true;
    }

    ~SharedPtrTheadSafe()
    {
        Dispose();
    }

    public SharedPtrTheadSafe(IntPtr nativeInstance)
    {
        OwnsAReference = true;
        NativeInstance = nativeInstance;
        AddSharedReference();
    }

    public static SharedPtrTheadSafe NewNulledSharedPtr (IntPtr nativeInstance)
    {
        SharedPtrTheadSafe result = new SharedPtrTheadSafe();
        result.NativeInstance = nativeInstance;
        result.ObjectPtr = IntPtr.Zero;
        result.ReferenceController = IntPtr.Zero;
        return result;
    }

    public static SharedPtrTheadSafe NonReferenceOwningSharedPtr (IntPtr nativeInstance)
    {
        SharedPtrTheadSafe result = new SharedPtrTheadSafe();
        result.OwnsAReference = false;
        result.NativeInstance = nativeInstance;
        return result;
    }

    private void AddSharedReference()
    {
        if (NativeInstance == IntPtr.Zero || ReferenceController == IntPtr.Zero)
        {
            return;
        }
            
        TSharedPtrExporter.CallAddSharedReference(ReferenceController);
    }

    private void ForceDecRef()
    {
        if (NativeInstance == IntPtr.Zero || ReferenceController == IntPtr.Zero)
        {
            return;
        }

        TSharedPtrExporter.CallReleaseSharedReference(ReferenceController);
    }

    public void CopyFrom(SharedPtrTheadSafe other)
    {
        ForceDecRef();
        ObjectPtr = other.ObjectPtr;
        ReferenceController = other.ReferenceController;
        AddSharedReference();
    }

    public override string ToString()
    {
        return "ShrPtr {" + ObjectPtr + ", " + ReferenceController + ":" + ReferenceCount + "}";
    }

    public void Dispose()
    {
        if (OwnsAReference)
        {
            ForceDecRef();
        }
            
        NativeInstance = IntPtr.Zero;
    }
}