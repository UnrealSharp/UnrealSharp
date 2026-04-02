using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

public abstract class MulticastDelegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : Delegate
{
    IntPtr _nativeProperty;
    IntPtr _nativeDelegate;

    public override void FromNative(IntPtr address, IntPtr nativeProperty)
    {
        // Keep a reference to the property and delegate for later usage
        _nativeDelegate = address;
        _nativeProperty = nativeProperty;
    }

    public override void ToNative(IntPtr address)
    {
        // This should never be called as unreal does not support passing a multicast delegate as a UFunction parameter.
        // But we have this here as a dummy for UProperties getter/setters
    }

    protected override void ProcessDelegate(IntPtr parameters)
    {
        FMulticastDelegatePropertyExporter.CallBroadcastDelegate(_nativeProperty, _nativeDelegate, parameters);
    }

    public override void BindUFunction(UObject targetObject, FName functionName)
    {
        FMulticastDelegatePropertyExporter.CallAddDelegate(_nativeProperty, _nativeDelegate, targetObject.NativeObject, functionName.ToString());
    }

    public override void BindUFunction(TWeakObjectPtr<UObject> targetObjectPtr, FName functionName)
    {
        if (!targetObjectPtr.Object.IsValid())
        {
            return;
        }
        
        BindUFunction(targetObjectPtr.Object!, functionName);
    }

    public override void Add(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            throw new ArgumentException("The callback for a multicast delegate must be a valid UFunction defined on a UClass", nameof(handler));
        }
        
        FMulticastDelegatePropertyExporter.CallAddDelegate(_nativeProperty, _nativeDelegate, targetObject.NativeObject, handler.Method.Name);
    }

    public override void Remove(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            return;
        }
        
        FMulticastDelegatePropertyExporter.CallRemoveDelegate(_nativeProperty, _nativeDelegate, targetObject.NativeObject, handler.Method.Name);
    }

    public override bool Contains(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            return false;
        }
        
        return FMulticastDelegatePropertyExporter.CallContainsDelegate(_nativeProperty, _nativeDelegate, targetObject.NativeObject, handler.Method.Name).ToManagedBool();
    }

    public override bool IsBound => FMulticastDelegatePropertyExporter.CallIsBound(_nativeDelegate).ToManagedBool();

    public override void Clear()
    {
        FMulticastDelegatePropertyExporter.CallClearDelegate(_nativeProperty, _nativeDelegate);
    }
}