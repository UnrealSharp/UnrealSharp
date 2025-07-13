using System.Runtime.InteropServices;

namespace UnrealSharp.Core;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ManagedCallbacks
{
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr> ScriptManagerBridge_CreateManagedObject;
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int> ScriptManagerBridge_InvokeManagedMethod;
    public delegate* unmanaged<IntPtr, void> ScriptManagerBridge_InvokeDelegate;
    public delegate* unmanaged<IntPtr, char*, IntPtr> ScriptManagerBridge_LookupManagedMethod;
    public delegate* unmanaged<IntPtr, char*, IntPtr> ScriptManagedBridge_LookupManagedType;
    public delegate* unmanaged<IntPtr, IntPtr, void> ScriptManagedBridge_Dispose;
    public delegate* unmanaged<IntPtr, void> ScriptManagedBridge_FreeHandle;

    public static void Initialize(IntPtr outManagedCallbacks)
    {
        *(ManagedCallbacks*)outManagedCallbacks = new ManagedCallbacks
        {
            ScriptManagerBridge_CreateManagedObject = &UnmanagedCallbacks.CreateNewManagedObject,
            ScriptManagerBridge_InvokeManagedMethod = &UnmanagedCallbacks.InvokeManagedMethod,
            ScriptManagerBridge_InvokeDelegate = &UnmanagedCallbacks.InvokeDelegate,
            ScriptManagerBridge_LookupManagedMethod = &UnmanagedCallbacks.LookupManagedMethod,
            ScriptManagedBridge_LookupManagedType = &UnmanagedCallbacks.LookupManagedType,
            ScriptManagedBridge_Dispose = &UnmanagedCallbacks.Dispose,
            ScriptManagedBridge_FreeHandle = &UnmanagedCallbacks.FreeHandle,
        };
    }
}