using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

[Binding]
public abstract class MulticastDelegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : Delegate
{
    protected IntPtr NativeProperty;
    protected IntPtr NativeDelegate;

    public override void FromNative(IntPtr address, IntPtr nativeProperty)
    {
        // Keep a reference to the property and delegate for later usage
        NativeDelegate = address;
        NativeProperty = nativeProperty;
    }

    public override void ToNative(IntPtr address)
    {
        // This should never be called as unreal does not support passing a multicast delegate as a UFunction parameter.
        // But we have this here as a dummy for UProperties getter/setters
    }

    protected override void ProcessDelegate(IntPtr parameters)
    {
        FMulticastDelegatePropertyExporter.CallBroadcastDelegate(NativeProperty, NativeDelegate, parameters);
    }

    public override void BindUFunction(UObject targetObject, FName functionName)
    {
        FMulticastDelegatePropertyExporter.CallAddDelegate(NativeProperty, NativeDelegate, targetObject.NativeObject, functionName.ToString());
    }

    public override void BindUFunction(TWeakObjectPtr<UObject> targetObjectPtr, FName functionName)
    {
        BindUFunction(targetObjectPtr.Object, functionName);
    }

    public override void Add(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            throw new ArgumentException("The callback for a multicast delegate must be a valid UFunction defined on a UClass", nameof(handler));
        }
        FMulticastDelegatePropertyExporter.CallAddDelegate(NativeProperty, NativeDelegate, targetObject.NativeObject, handler.Method.Name);
    }

    public override void Remove(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            return;
        }
        FMulticastDelegatePropertyExporter.CallRemoveDelegate(NativeProperty, NativeDelegate, targetObject.NativeObject, handler.Method.Name);
    }

    public override bool Contains(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            return false;
        }
        return FMulticastDelegatePropertyExporter.CallContainsDelegate(NativeProperty, NativeDelegate, targetObject.NativeObject, handler.Method.Name).ToManagedBool();
    }

    public override bool IsBound => FMulticastDelegatePropertyExporter.CallIsBound(NativeDelegate).ToManagedBool();

    public override void Clear()
    {
        FMulticastDelegatePropertyExporter.CallClearDelegate(NativeProperty, NativeDelegate);
    }
}