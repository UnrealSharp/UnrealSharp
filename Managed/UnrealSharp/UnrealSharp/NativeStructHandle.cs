using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

public class NativeStructHandle(IntPtr nativeScriptStruct) : SafeHandle(IntPtr.Zero, true)
{
    private IntPtr _nativeStructPtr = UScriptStructExporter.CallAllocateNativeStruct(nativeScriptStruct);

    public IntPtr NativeStructPtr
    {
        get
        {
            if (_nativeStructPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Native struct handle is invalid");
            }
            
            return _nativeStructPtr;
        }
    }

    public override bool IsInvalid => NativeStructPtr == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (NativeStructPtr == IntPtr.Zero) return true;
        
        UScriptStructExporter.CallDeallocateNativeStruct(nativeScriptStruct, NativeStructPtr);
        _nativeStructPtr = IntPtr.Zero;

        return true;
    }
}