using UnrealSharp.CoreUObject;

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
    
    public abstract void BindUFunction(UObject targetObject, FName functionName);
    public abstract void BindUFunction(TWeakObjectPtr<UObject> targetObjectPtr, FName functionName);
}

public class DelegateMarshaller<TDelegate> where TDelegate : IDelegateBase, new()
{
    public static TDelegate FromNative(IntPtr nativeBuffer, IntPtr nativeProperty, int arrayIndex)
    {
        TDelegate managedDelegate = new TDelegate();
        managedDelegate.FromNative(nativeBuffer, nativeProperty);
        return managedDelegate;
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, object obj)
    {
        if (obj is not IDelegateBase @delegate)
        {
            return;
        }
        @delegate.ToNative(nativeBuffer);
    }
}