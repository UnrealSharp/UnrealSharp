using UnrealSharp.Interop;

namespace UnrealSharp;

public abstract class IDelegateBase
{
    public abstract void FromNative(IntPtr address);
}

public abstract class DelegateBase<TDelegate> : IDelegateBase where TDelegate : class
{
    public TDelegate Invoke => GetInvoker();

    protected virtual TDelegate GetInvoker()
    {
        return null;
    }

    public override void FromNative(IntPtr address)
    {
        NativeDelegate = address;
    }

    protected void ProcessDelegate(IntPtr parameters)
    {
        FMulticastDelegatePropertyExporter.CallBroadcastDelegate(NativeDelegate, parameters);
    }

    protected IntPtr NativeDelegate;
}

public class DelegateMarshaller<TDelegate> where TDelegate : IDelegateBase, new()
{
    public static TDelegate FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        var managedDelegate = new TDelegate();
        managedDelegate.FromNative(nativeBuffer);
        return managedDelegate;
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, object obj)
    {

    }
}