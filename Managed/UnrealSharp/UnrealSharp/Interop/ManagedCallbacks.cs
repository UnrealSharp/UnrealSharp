using System.Runtime.InteropServices;

namespace UnrealSharp.Interop;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ManagedCallbacks
{
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr> ScriptManagerBridge_CreateManagedObject;
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int> ScriptManagerBridge_InvokeManagedMethod;
    public delegate* unmanaged<IntPtr, char*, IntPtr> ScriptManagerBridge_LookupManagedMethod;
    public delegate* unmanaged<IntPtr, char*, char*, IntPtr> ScriptManagedBridge_LookupManagedType;
    public delegate* unmanaged<IntPtr, void> ScriptManagedBridge_Dispose;

    public static ManagedCallbacks Create()
    {
        return new()
        {
            ScriptManagerBridge_CreateManagedObject = &UnmanagedCallbacks.CreateNewManagedObject,
            ScriptManagerBridge_InvokeManagedMethod = &UnmanagedCallbacks.InvokeManagedMethod,
            ScriptManagerBridge_LookupManagedMethod = &UnmanagedCallbacks.LookupManagedMethod,
            ScriptManagedBridge_LookupManagedType = &UnmanagedCallbacks.LookupManagedType,
            ScriptManagedBridge_Dispose = &UnmanagedCallbacks.Dispose,
        };
    }

    public static void Create(IntPtr outManagedCallbacks) => *(ManagedCallbacks*)outManagedCallbacks = Create();
}