using UnrealSharp.Interop;
using Object = UnrealSharp.CoreUObject.Object;

namespace UnrealSharp;

public abstract class IDelegateBase
{
    public abstract void FromNative(IntPtr address, IntPtr nativeProperty);
}

public abstract class DelegateBase<TDelegate> : IDelegateBase where TDelegate : class
{
    public TDelegate Invoke => GetInvoker();
    protected IntPtr NativeProperty;

    protected virtual TDelegate GetInvoker()
    {
        return null;
    }

    public override void FromNative(IntPtr address, IntPtr nativeProperty)
    {
        NativeDelegate = address;
        NativeProperty = nativeProperty;
    }

    protected abstract void ProcessDelegate(IntPtr parameters);
    
    public abstract void BindUFunction(Object targetObject, Name functionName);
    public abstract void BindUFunction(WeakObject<Object> targetObject, Name functionName);
    
    public void Clear()
    {
        FMulticastDelegatePropertyExporter.CallClearDelegate(NativeDelegate, NativeProperty);
    }

    protected IntPtr NativeDelegate;
}

public class DelegateMarshaller<TDelegate> where TDelegate : IDelegateBase, new()
{
    public static TDelegate FromNative(IntPtr nativeBuffer, IntPtr nativeProperty, int arrayIndex, UnrealSharpObject owner)
    {
        TDelegate managedDelegate = new TDelegate();
        managedDelegate.FromNative(nativeBuffer, nativeProperty);
        return managedDelegate;
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, object obj)
    {

    }
}