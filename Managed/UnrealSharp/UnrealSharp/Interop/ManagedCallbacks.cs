using System.Runtime.InteropServices;

namespace UnrealSharp.Interop;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ManagedCallbacks
{
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr> CreateManagedObject;
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, void> InvokeManagedMethod;
    public delegate* unmanaged<IntPtr, char*, IntPtr> LookupManagedMethod;
    public delegate* unmanaged<IntPtr, char*, char*, IntPtr> LookupManagedType;
    public delegate* unmanaged<IntPtr, void> Dispose;

    public static ManagedCallbacks Create()
    {
        return new ManagedCallbacks
        {
            CreateManagedObject = &UnmanagedCallbacks.CreateNewManagedObject,
            InvokeManagedMethod = &UnmanagedCallbacks.InvokeManagedMethod,
            LookupManagedMethod = &UnmanagedCallbacks.LookupManagedMethod,
            LookupManagedType = &UnmanagedCallbacks.LookupManagedType,
            Dispose = &UnmanagedCallbacks.Dispose,
        };
    }

    public static void Create(IntPtr outManagedCallbacks) => *(ManagedCallbacks*)outManagedCallbacks = Create();
}