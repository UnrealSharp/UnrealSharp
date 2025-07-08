using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

public class NativeStructHandle(IntPtr nativeScriptStruct) : SafeHandle(IntPtr.Zero, true)
{
    public IntPtr NativeStructPtr { get; private set; } = UScriptStructExporter.CallAllocateNativeStruct(nativeScriptStruct);

    public override bool IsInvalid => NativeStructPtr == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (NativeStructPtr == IntPtr.Zero) return true;
        
        UScriptStructExporter.CallDeallocateNativeStruct(nativeScriptStruct, NativeStructPtr);
        NativeStructPtr = IntPtr.Zero;

        return true;
    }
}