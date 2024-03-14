using UnrealSharp.Interop;

namespace UnrealSharp;

public abstract class MulticastDelegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : class
{
    protected override void ProcessDelegate(IntPtr parameters)
    {
        FMulticastDelegatePropertyExporter.CallBroadcastDelegate(NativeProperty, NativeDelegate, parameters);
    }

    public override void BindUFunction(UnrealSharpObject targetObject, Name functionName)
    {
        FMulticastDelegatePropertyExporter.CallAddDelegate(NativeProperty, NativeDelegate, targetObject.NativeObject, functionName.ToString());
    }

    public override void BindUFunction(WeakObject<UnrealSharpObject> targetObject, Name functionName)
    {
        BindUFunction(targetObject.Object, functionName);
    }
}