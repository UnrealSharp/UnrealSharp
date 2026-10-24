using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_UScriptStruct
{
    public static delegate* unmanaged<IntPtr, int> GetNativeStructSize;

    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, NativeBool> NativeCopy;
    
    public static delegate* unmanaged<IntPtr, IntPtr, NativeBool> NativeDestroy;

    public static delegate* unmanaged<ref NativeStructHandleData, IntPtr, void> AllocateNativeStruct;

    public static delegate* unmanaged<ref NativeStructHandleData, IntPtr, void> DeallocateNativeStruct;
    
    public static delegate* unmanaged<NativeStructHandleData*, IntPtr, IntPtr> GetStructLocation;
    
    public static delegate* unmanaged<IntPtr, IntPtr> GetManagedStructType;
}