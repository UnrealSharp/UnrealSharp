using UnrealSharp.Interop;
using Object = UnrealSharp.CoreUObject.Object;

namespace UnrealSharp;

public interface IDelegateBase
{
    void FromNative(IntPtr address, IntPtr nativeProperty);

    void ToNative(IntPtr address);
}

public abstract class DelegateBase<TDelegate> : IDelegateBase where TDelegate : Delegate
{
    public TDelegate Invoke => GetInvoker();

    protected virtual TDelegate GetInvoker()
    {
        return null;
    }

    public abstract void FromNative(IntPtr address, IntPtr nativeProperty);

    public abstract void ToNative(IntPtr address);

    protected abstract void ProcessDelegate(IntPtr parameters);
    
    public abstract void BindUFunction(Object targetObject, Name functionName);
    public abstract void BindUFunction(WeakObject<Object> targetObject, Name functionName);
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
        if (obj is not IDelegateBase @delegate)
        {
            return;
        }
        @delegate.ToNative(nativeBuffer);
    }
}