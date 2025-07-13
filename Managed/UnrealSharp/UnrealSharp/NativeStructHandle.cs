using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

[InlineArray(64)]
public struct NativeStructHandleData
{
    private byte _data;
}

public sealed class NativeStructHandle : IDisposable
{
    private NativeStructHandleData _nativeStructHandleData;
    private IntPtr _nativeScriptStruct;

    public NativeStructHandle(IntPtr nativeScriptStruct)
    {
        UScriptStructExporter.CallAllocateNativeStruct(ref _nativeStructHandleData, nativeScriptStruct);
        _nativeScriptStruct = nativeScriptStruct;
    }
    
    public ref NativeStructHandleData Data => ref _nativeStructHandleData;

    ~NativeStructHandle()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_nativeScriptStruct == IntPtr.Zero) return;
        
        UScriptStructExporter.CallDeallocateNativeStruct(ref _nativeStructHandleData, _nativeScriptStruct);
        _nativeScriptStruct = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}