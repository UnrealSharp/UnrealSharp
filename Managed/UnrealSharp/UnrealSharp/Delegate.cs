using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct DelegateData
{
    public ulong Storage;
    public WeakObjectData Object;
    public FName FunctionName;
}

public abstract class Delegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : Delegate
{
    private DelegateData _data;
    
    public TWeakObject<UObject> TargetObject => new(_data.Object);
    public FName FunctionName => _data.FunctionName;
    
    public Delegate()
    {
    }
    
    public Delegate(DelegateData data)
    {
        _data = data;
    }
    
    public Delegate(UObject targetObject, FName functionName)
    {
        _data = new DelegateData
        {
            FunctionName = functionName
        };
        
        FWeakObjectPtrExporter.CallSetObject(ref _data.Object, targetObject.NativeObject);
    }

    public override void FromNative(IntPtr address, IntPtr nativeProperty)
    {
        // Copy the singlecast delegate data from the native property
        unsafe
        {
            _data = *(DelegateData*)address;
        }
    }

    public override void ToNative(IntPtr address)
    {
        // Copy the singlecast delegate data to the native property
        unsafe
        {
            *(DelegateData*)address = _data;
        }
    }

    public bool IsBoundToObject(Object targetObject)
    {
        return targetObject.Equals(TargetObject.Object);
    }

    public bool IsBoundTo(UObject targetObject, FName functionName)
    {
        return targetObject.Equals(TargetObject.Object) && FunctionName == functionName;
    }

    public override void BindUFunction(UObject targetObject, FName functionName)
    {
        BindUFunction(new TWeakObject<UObject>(targetObject), functionName);
    }

    public override void BindUFunction(TWeakObject<UObject> targetObject, FName functionName)
    {
        _data.Object = targetObject.Data;
        _data.FunctionName = functionName;
    }

    public void Add(TDelegate handler)
    {
        if (IsBound)
        {
            throw new InvalidOperationException($"A singlecast delegate can only be bound to one handler at a time. Unbind it first before binding a new handler.");
        }
        
        if (handler.Target is not UObject targetObject)
        {
            throw new ArgumentException("The callback for a singlecast delegate must be a valid UFunction defined on a UClass", nameof(handler));
        }
        
        _data.Object = new TWeakObject<UObject>(targetObject).Data;
        _data.FunctionName = new FName(handler.Method.Name);
    }

    public void Remove(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            return;
        }
        
        if (!IsBoundTo(targetObject, handler.Method.Name))
        {
            return;
        }
        Unbind();
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