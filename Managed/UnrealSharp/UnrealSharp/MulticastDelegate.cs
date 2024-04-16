using UnrealSharp.Interop;
using Object = UnrealSharp.CoreUObject.Object;

namespace UnrealSharp;

public abstract class MulticastDelegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : class
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
        // This should never be called as unreal does not support passing a multicast delegate as a UFunction parameter
        throw new NotImplementedException();
    }

    protected override void ProcessDelegate(IntPtr parameters)
    {
        FMulticastDelegatePropertyExporter.CallBroadcastDelegate(NativeProperty, NativeDelegate, parameters);
    }

    public override void BindUFunction(Object targetObject, Name functionName)
    {
        FMulticastDelegatePropertyExporter.CallAddDelegate(NativeProperty, NativeDelegate, targetObject.NativeObject, functionName.ToString());
    }

    public override void BindUFunction(WeakObject<Object> targetObject, Name functionName)
    {
        BindUFunction(targetObject.Object, functionName);
    }

    public void Clear()
    {
        FMulticastDelegatePropertyExporter.CallClearDelegate(NativeDelegate, NativeProperty);
    }
}