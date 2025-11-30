using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

public abstract class Delegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : Delegate
{
    public TWeakObjectPtr<UObject> TargetObject;
    public FName FunctionName;
    
    public Delegate()
    {
        
    }
    
    public Delegate(UObject targetObject, FName functionName)
    {
        TargetObject = new TWeakObjectPtr<UObject>(targetObject);
        FunctionName = functionName;
    }

    public override void FromNative(IntPtr address, IntPtr nativeProperty)
    {
        FScriptDelegateExporter.CallGetDelegateInfo(address, out IntPtr targetObjectPtr, out FName functionName);
        TargetObject = new TWeakObjectPtr<UObject>(targetObjectPtr);
        FunctionName = functionName;
    }

    public override void ToNative(IntPtr address)
    {
        UObject? targetObject = TargetObject.Object;
        FScriptDelegateExporter.CallMakeDelegate(address, targetObject?.NativeObject ?? IntPtr.Zero, FunctionName);
    }

    public override bool Contains(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            return false;
        }
        
        return targetObject.Equals(TargetObject.Object) && FunctionName == handler.Method.Name;
    }
    
    public override void BindUFunction(UObject targetObject, FName functionName)
    {
        BindUFunction(new TWeakObjectPtr<UObject>(targetObject), functionName);
    }

    public override void BindUFunction(TWeakObjectPtr<UObject> targetObjectPtr, FName functionName)
    {
        TargetObject = targetObjectPtr;
        FunctionName = functionName;
    }

    public override void Add(TDelegate handler)
    {
        if (IsBound)
        {
            throw new InvalidOperationException($"A singlecast delegate can only be bound to one handler at a time. Unbind it first before binding a new handler.");
        }
        
        if (handler.Target is not UObject targetObject)
        {
            throw new ArgumentException("The callback for a singlecast delegate must be a valid UFunction defined on a UClass", nameof(handler));
        }
        
        TargetObject = new TWeakObjectPtr<UObject>(targetObject);
        FunctionName = new FName(handler.Method.Name);
    }

    public override void Remove(TDelegate handler)
    {
        if (!Contains(handler))
        {
            return;
        }
        
        Clear();
    }

    public override bool IsBound => TargetObject.IsValid && !FunctionName.IsNone;

    public override void Clear()
    {
        TargetObject = new TWeakObjectPtr<UObject>();
        FunctionName = FName.None;
    }

    public override string ToString()
    {
        return $"{TargetObject.Object}::{FunctionName}";
    }

    protected override void ProcessDelegate(IntPtr parameters)
    {
        UObject? targetObject = TargetObject.Object;
        
        if (targetObject == null)
        {
            LogUnrealSharp.LogWarning($"Attempted to invoke delegate, but target object is null. Delegate: {this}");
            return;
        }
        
        FScriptDelegateExporter.CallBroadcastDelegate(targetObject.NativeObject, FunctionName, parameters);
    }
}