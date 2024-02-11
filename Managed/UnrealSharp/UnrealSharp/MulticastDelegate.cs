using UnrealSharp.Interop;

namespace UnrealSharp;

public abstract class MulticastDelegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : class
{
    protected override void ProcessDelegate(IntPtr parameters)
    {
        FMulticastDelegatePropertyExporter.CallBroadcastDelegate(NativeDelegate, NativeProperty, parameters);
    }
}