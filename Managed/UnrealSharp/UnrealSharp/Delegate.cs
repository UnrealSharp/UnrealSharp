using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct DelegateData
{
    public WeakObjectData Object;
    public Name FunctionName;
}

public abstract class Delegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : class
{
    private readonly DelegateData _data;
    
    internal Delegate(DelegateData data)
    {
        _data = data;
    }
    
    public Delegate(CoreUObject.Object targetObject, Name functionName)
    {
        _data = new DelegateData
        {
            FunctionName = functionName
        };
        
        FWeakObjectPtrExporter.CallSetObject(ref _data.Object, targetObject.NativeObject);
    }

    public Delegate()
    {
        
    }
}

public class SimpleDelegateMarshaller<TDelegate> where TDelegate : TDelegate, new()
{
    public static TDelegate FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        return BlittableMarshaller<TDelegate>.FromNative(nativeBuffer, arrayIndex, owner);
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, TDelegate obj)
    {
        BlittableMarshaller<TDelegate>.ToNative(nativeBuffer, arrayIndex, owner, obj);
    }
}