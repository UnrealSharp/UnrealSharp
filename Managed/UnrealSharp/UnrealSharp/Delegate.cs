using System.Runtime.InteropServices;
using UnrealSharp.Interop;
using Object = UnrealSharp.CoreUObject.Object;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct DelegateData
{
    public ulong Storage;
    public WeakObjectData Object;
    public Name FunctionName;
}

public abstract class Delegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : class
{
    private DelegateData _data;
    
    public WeakObject<CoreUObject.Object> TargetObject => new(_data.Object);
    public Name FunctionName => _data.FunctionName;
    
    public Delegate()
    {
    }
    
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

    public bool IsBoundToObject(CoreUObject.Object obj)
    {
        return obj.Equals(TargetObject.Object);
    }

    public override void BindUFunction(Object targetObject, Name functionName)
    {
        BindUFunction(new WeakObject<Object>(targetObject), functionName);
    }

    public override void BindUFunction(WeakObject<Object> targetObject, Name functionName)
    {
        _data.Object = targetObject._data;
        _data.FunctionName = functionName;
    }

    public bool IsBound => _data.Object.ObjectIndex != 0;
    
    public void Unbind()
    {
        _data.Object = default;
        _data.FunctionName = default;
        _data.Storage = 0;
    }
    
    public override string ToString()
    {
        return $"{TargetObject.Object}::{FunctionName}";
    }

    protected override void ProcessDelegate(IntPtr parameters)
    {
        FScriptDelegateExporter.CallBroadcastDelegate(ref _data, parameters);
    }
}

public partial class MyTestDelegate : Delegate<MyTestDelegate.MyDelegateSignature>
{
    public delegate void MyDelegateSignature();
}