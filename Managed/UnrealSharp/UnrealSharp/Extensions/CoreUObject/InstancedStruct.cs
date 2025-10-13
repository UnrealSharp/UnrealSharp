using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Sequential)]
public struct FInstancedStructData
{
    private readonly IntPtr _scriptStruct;
    private readonly IntPtr _structMemory;
}

internal sealed class FInstancedStructManager : IDisposable
{
    private FInstancedStructData _structData;
    
    public ref FInstancedStructData StructData => ref _structData;

    public FInstancedStructManager()
    {
        FInstancedStructExporter.CallNativeInit(ref _structData);
    }

    public FInstancedStructManager(IntPtr data)
    {
        unsafe
        {
            var structData = (FInstancedStructData*)data;
            FInstancedStructExporter.CallNativeCopy(ref _structData, ref *structData);
        }
    }

    ~FInstancedStructManager()
    {
        Dispose();   
    }
    
    public void Dispose()
    {
        FInstancedStructExporter.CallNativeDestroy(ref _structData);
        GC.SuppressFinalize(this);   
    }
}

[UStruct, GeneratedType("InstancedStruct", "UnrealSharp.CoreUObject.InstancedStruct")]
public struct FInstancedStruct : MarshalledStruct<FInstancedStruct>, IDisposable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private FInstancedStructManager? _manager;
    
    private static readonly IntPtr NativeClassPtr;
    public static IntPtr GetNativeClassPtr() => NativeClassPtr;

    public static readonly int NativeDataSize;
    public static int GetNativeDataSize() => NativeDataSize;
    
    static FInstancedStruct()
    {
        NativeClassPtr = UCoreUObjectExporter.CallGetNativeStructFromName(typeof(FInstancedStruct).GetAssemblyName(), "UnrealSharp.CoreUObject", "InstancedStruct");
        NativeDataSize = UScriptStructExporter.CallGetNativeStructSize(NativeClassPtr);
    }

    public FInstancedStruct()
    {
        _manager = new FInstancedStructManager();   
    }

    public FInstancedStruct(IntPtr inNativeStruct)
    {
        _manager = new FInstancedStructManager(inNativeStruct);
    }

    public UScriptStruct? ScriptStruct
    {
        get
        {
            _manager ??= new FInstancedStructManager();  
            var nativeStruct = FInstancedStructExporter.CallGetNativeStruct(ref _manager.StructData);
            IntPtr handle = FCSManagerExporter.CallFindManagedObject(nativeStruct);
            return GCHandleUtilities.GetObjectFromHandlePtr<UScriptStruct>(handle);
        }
    }

    public static FInstancedStruct Make<T>() where T : struct, MarshalledStruct<T>
    {
        var instancedStruct = new FInstancedStruct();
        FInstancedStructExporter.CallInitializeAs(ref instancedStruct._manager!.StructData, T.GetNativeClassPtr(), IntPtr.Zero);
        return instancedStruct;   
    }

    public static FInstancedStruct Make<T>(T value) where T : struct, MarshalledStruct<T>
    {
        unsafe
        {
            var instancedStruct = new FInstancedStruct();
            Span<byte> data = stackalloc byte[T.GetNativeDataSize()];
            IntPtr nativeStruct = T.GetNativeClassPtr();
            fixed (byte* dataPtr = data)
            {
                value.ToNative((IntPtr) dataPtr);
                FInstancedStructExporter.CallInitializeAs(ref instancedStruct._manager!.StructData, nativeStruct, (IntPtr)dataPtr);
                UScriptStructExporter.CallNativeDestroy(nativeStruct, (IntPtr)dataPtr);
            }
            return instancedStruct; 
        }
    }

    [MemberNotNull(nameof(_manager))]
    public bool IsA(UScriptStruct scriptStruct)
    {
        return IsA(scriptStruct.NativeObject);
    }

    [MemberNotNull(nameof(_manager))]
    public bool IsA<T>() where T : struct, MarshalledStruct<T>
    {
        return IsA(T.GetNativeClassPtr()); 
    }

    [MemberNotNull(nameof(_manager))]
    private bool IsA(IntPtr scriptStruct)
    {
        _manager ??= new FInstancedStructManager(); 
        IntPtr nativeStruct = FInstancedStructExporter.CallGetNativeStruct(ref _manager.StructData);
        return nativeStruct == scriptStruct;
    }

    public T Get<T>() where T : struct, MarshalledStruct<T>
    {
        return TryGet(out T value) ? value : throw new InvalidOperationException($"Failed to get {typeof(T).Name} from instanced struct.");  
    }

    public bool TryGet<T>(out T value) where T : struct, MarshalledStruct<T>
    {
        if (!IsA<T>())
        {
            value = default;
            return false;
        }
        
        value = T.FromNative(FInstancedStructExporter.CallGetMemory(ref _manager.StructData));
        return true;   
    }

    public static FInstancedStruct FromNative(IntPtr buffer)
    {
        return new FInstancedStruct(buffer);
    }

    public void ToNative(IntPtr buffer)
    {
        _manager ??= new FInstancedStructManager();   
        unsafe
        {
            var structData = (FInstancedStructData*)buffer;
            FInstancedStructExporter.CallNativeCopy(ref *structData, ref _manager.StructData);
        }
    }


    public void Dispose()
    {
        _manager?.Dispose();
    }
}

public static class FInstancedStructMarshaller
{
    public static FInstancedStruct FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        return new FInstancedStruct(nativeBuffer + arrayIndex * GetNativeDataSize());
    }
    
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, FInstancedStruct obj)
    {
        obj.ToNative(nativeBuffer + (arrayIndex * GetNativeDataSize()));
    }
    
    public static int GetNativeDataSize()
    {
        return FInstancedStruct.NativeDataSize;
    }
}