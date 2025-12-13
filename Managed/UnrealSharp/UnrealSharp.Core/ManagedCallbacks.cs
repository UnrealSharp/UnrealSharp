using System.Runtime.InteropServices;

namespace UnrealSharp.Core;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ManagedCallbacks
{   
    public delegate* unmanaged<IntPtr, IntPtr, char**, IntPtr> CreateManagedObject;
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr> CreateNewManagedObjectWrapper;
    
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int> InvokeManagedMethod;
    
    public delegate* unmanaged<IntPtr, void> InvokeDelegate;
    public delegate* unmanaged<IntPtr, char*, IntPtr> LookupManagedMethod;
    public delegate* unmanaged<IntPtr, char*, IntPtr> LookupManagedType;
    public delegate* unmanaged<IntPtr, IntPtr, void> Dispose;
    public delegate* unmanaged<IntPtr, void> FreeHandle;

    public static void Initialize(IntPtr outManagedCallbacks)
    {
        *(ManagedCallbacks*)outManagedCallbacks = new ManagedCallbacks
        {
            CreateManagedObject = &UnmanagedCallbacks.CreateNewManagedObject,
            CreateNewManagedObjectWrapper = &UnmanagedCallbacks.CreateNewManagedObjectWrapper,
            InvokeManagedMethod = &UnmanagedCallbacks.InvokeManagedMethod,
            InvokeDelegate = &UnmanagedCallbacks.InvokeDelegate,
            LookupManagedMethod = &UnmanagedCallbacks.LookupManagedMethod,
            LookupManagedType = &UnmanagedCallbacks.LookupManagedType,
            Dispose = &UnmanagedCallbacks.Dispose,
            FreeHandle = &UnmanagedCallbacks.FreeHandle,
        };
    }
}