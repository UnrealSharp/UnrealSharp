using UnrealSharp.Interop;
using Object = UnrealSharp.CoreUObject.Object;

namespace UnrealSharp;

public abstract class MulticastDelegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : class
{
    protected override void ProcessDelegate(IntPtr parameters)
    {
        FMulticastDelegatePropertyExporter.CallBroadcastDelegate(NativeDelegate, NativeProperty, parameters);
    }

    public override void BindUFunction(Object targetObject, Name functionName)
    {
        FMulticastDelegatePropertyExporter.CallAddDelegate(NativeProperty, NativeDelegate, targetObject.NativeObject, functionName.ToString());
    }

    public override void BindUFunction(WeakObject<Object> targetObject, Name functionName)
    {
        BindUFunction(targetObject.Object, functionName);
    }
}