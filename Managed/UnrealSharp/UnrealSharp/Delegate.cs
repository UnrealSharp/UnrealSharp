using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct DelegateData
{
    private ulong Storage;
    public WeakObjectData Object;
    public Name FunctionName;
}

public abstract class Delegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : class
{
    private DelegateData _data;
    
    public WeakObject<CoreUObject.Object> TargetObject => new(_data.Object);
    public Name FunctionName => _data.FunctionName;
    
    public Delegate(DelegateData data)
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