using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UScriptStructExporter
{
    public static delegate* unmanaged<IntPtr, int> GetNativeStructSize;

    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, bool> NativeCopy;
    
    public static delegate* unmanaged<IntPtr, IntPtr, bool> NativeDestroy;

    public static delegate* unmanaged<ref NativeStructHandleData, IntPtr, void> AllocateNativeStruct;

    public static delegate* unmanaged<ref NativeStructHandleData, IntPtr, void> DeallocateNativeStruct;
    
    public static delegate* unmanaged<NativeStructHandleData*, IntPtr, IntPtr> GetStructLocation;
}