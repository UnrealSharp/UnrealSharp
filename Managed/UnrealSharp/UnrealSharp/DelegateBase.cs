using UnrealSharp.Core;
using UnrealSharp.CoreUObject;

namespace UnrealSharp;

public abstract class DelegateBase<TDelegate> where TDelegate : Delegate
{
    public TDelegate Invoke => GetInvoker();

    protected abstract TDelegate GetInvoker();

    public abstract void FromNative(IntPtr address, IntPtr nativeProperty);
    public abstract void ToNative(IntPtr address);

    protected abstract void ProcessDelegate(IntPtr parameters);
    
    public abstract void BindUFunction(UObject targetObject, FName functionName);
    public abstract void BindUFunction(TWeakObjectPtr<UObject> targetObjectPtr, FName functionName);
    
    public abstract void Remove(TDelegate handler);
    public abstract void Add(TDelegate handler);
    
    public abstract bool Contains(TDelegate handler);
    public abstract void Clear();

    // <summary>
    /// Checks if the delegate is bound to any UObject.
    /// </summary>
    public virtual bool IsBound => throw new NotImplementedException();
}

internal class DelegateMarshaller<TWrapperDelegate, TDelegate> where TWrapperDelegate : TDelegateBase<TDelegate>, new() where TDelegate : Delegate
{
    public static TWrapperDelegate FromNative(IntPtr nativeBuffer, IntPtr nativeProperty, int arrayIndex)
    {
        TWrapperDelegate managedDelegate = new TWrapperDelegate();
        managedDelegate.InnerDelegate.FromNative(nativeBuffer, nativeProperty);
        return managedDelegate;
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, object obj)
    {
        if (obj is not TWrapperDelegate @delegate)
        {
            return;
        }
        
        @delegate.InnerDelegate.ToNative(nativeBuffer);
    }
}

public class MulticastDelegateMarshaller<T> where T : Delegate
{
    public static TMulticastDelegate<T> FromNative(IntPtr nativeBuffer, IntPtr nativeProperty, int arrayIndex)
    {
        return DelegateMarshaller<TMulticastDelegate<T>, T>.FromNative(nativeBuffer, nativeProperty, arrayIndex);
    }
    
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, object obj)
    {
        DelegateMarshaller<TMulticastDelegate<T>, T>.ToNative(nativeBuffer, arrayIndex, obj);
    }
}

public class SingleDelegateMarshaller<T> where T : Delegate
{
    public static TDelegate<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        return DelegateMarshaller<TDelegate<T>, T>.FromNative(nativeBuffer, IntPtr.Zero, arrayIndex);
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, object obj)
    {
        DelegateMarshaller<TDelegate<T>, T>.ToNative(nativeBuffer, arrayIndex, obj);
    }
}

public abstract class TDelegateBase<T> where T : Delegate
{
    private static readonly Type Wrapper;
    
    public readonly DelegateBase<T> InnerDelegate;
    
    static TDelegateBase()
    {
        Type delegateType = typeof(T);
        string wrapperName = $"{delegateType.Name}__DelegateSignature";
        string fullName = $"{delegateType.Namespace}.{wrapperName}";
        
        Type? foundWrapper = delegateType.Assembly.GetType(fullName);

        if (foundWrapper == null)
        {
            throw new TypeLoadException($"Could not find wrapper type '{fullName}' for '{typeof(T).FullName}'");
        }
        
        Wrapper = foundWrapper;
    }

    internal TDelegateBase()
    {
        InnerDelegate = (DelegateBase<T>) Activator.CreateInstance(Wrapper);
    }
    
    /// <summary>
    /// Adds a function to the delegate.
    /// Recommend using + operator instead.
    /// </summary>
    public void Add(T handler)
    {
        InnerDelegate.Add(handler);
    }
    
    /// <summary>
    /// Checks if the delegate is bound to any UObject.
    /// </summary>
    public bool IsBound => InnerDelegate.IsBound;
    
    /// <summary>
    /// Removes a function from the delegate.
    /// Recommend using - operator instead.
    /// </summary>
    public void Remove(T handler)
    {
        InnerDelegate.Remove(handler);
    }
    
    /// <summary>
    /// Checks if the delegate contains a callback.
    /// </summary>
    public bool Contains(T handler)
    {
        return InnerDelegate.Contains(handler);
    }
    
    /// <summary>
    /// Clears all functions from the delegate.
    /// </summary>
    public void Clear()
    {
        InnerDelegate.Clear();
    }
    
    /// <summary>
    /// Binds a UFunction to the delegate.
    /// Recommend using + operator instead.
    /// </summary>
    /// <param name="targetObject">The UObject to bind the function to.</param>
    /// <param name="functionName">The name of the function to bind.</param>
    public void BindUFunction(UObject targetObject, FName functionName)
    {
        InnerDelegate.BindUFunction(targetObject, functionName);
    }

    /// <summary>
    /// Binds a UFunction to the delegate.
    /// Recommend using + operator instead.
    /// </summary>
    /// <param name="targetObjectPtr">The UObject to bind the function to.</param>
    /// <param name="functionName">The name of the function to bind.</param>
    public void BindUFunction(TWeakObjectPtr<UObject> targetObjectPtr, FName functionName)
    {
        InnerDelegate.BindUFunction(targetObjectPtr, functionName);
    }
}

public class TMulticastDelegate<T> : TDelegateBase<T> where T : Delegate
{
    public static TMulticastDelegate<T> operator +(TMulticastDelegate<T> thisDelegate, T handler)
    {
        thisDelegate.InnerDelegate.Add(handler);
        return thisDelegate;
    }

    public static TMulticastDelegate<T> operator -(TMulticastDelegate<T> thisDelegate, T handler)
    {
        thisDelegate.InnerDelegate.Remove(handler);
        return thisDelegate;
    }
};

public class TDelegate<T> : TDelegateBase<T> where T : Delegate
{
    public static TDelegate<T> operator +(TDelegate<T> thisDelegate, T handler)
    {
        thisDelegate.InnerDelegate.Add(handler);
        return thisDelegate;
    }

    public static TDelegate<T> operator -(TDelegate<T> thisDelegate, T handler)
    {
        thisDelegate.InnerDelegate.Remove(handler);
        return thisDelegate;
    }
};
