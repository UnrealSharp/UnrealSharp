using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

public class NativeStructHandle : SafeHandle
{
    private byte[]? _nativeStructPtr;
    private readonly IntPtr _nativeScriptStruct;

    public NativeStructHandle(IntPtr nativeScriptStruct) : base(IntPtr.Zero, true)
    {
        unsafe
        {
            _nativeStructPtr = new byte[UScriptStructExporter.CallGetNativeStructSize(nativeScriptStruct)];
            _nativeScriptStruct = nativeScriptStruct;
            fixed (byte* structPtr = _nativeStructPtr)
            {
                UStructExporter.CallInitializeStruct(nativeScriptStruct, (IntPtr) structPtr);
            }
        }
    }

    public byte[] NativeStructPtr
    {
        get
        {
            if (_nativeStructPtr is null)
            {
                throw new InvalidOperationException("Native struct handle is invalid");
            }
            
            return _nativeStructPtr;
        }
    }

    public override bool IsInvalid => _nativeStructPtr is null;

    protected override bool ReleaseHandle()
    {
        unsafe
        {
            if (_nativeStructPtr is null) return true;
        
            fixed (byte* structPtr = _nativeStructPtr)
            {
                UScriptStructExporter.CallNativeDestroy(_nativeScriptStruct, (IntPtr) structPtr);
            }
            _nativeStructPtr = null;

            return true;
        }
    }
}