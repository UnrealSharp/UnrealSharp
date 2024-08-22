using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp;

public interface IDelegateBase
{
    void FromNative(IntPtr address, IntPtr nativeProperty);

    void ToNative(IntPtr address);
}

[Binding]
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
    
    public abstract void Remove(TDelegate handler);
    public abstract void Add(TDelegate handler);
    
    public abstract bool Contains(TDelegate handler);
    public abstract void Clear();
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

public class UDelegate<T> where T : Delegate
{
    static readonly Type Wrapper;
    public DelegateBase<T> InnerDelegate;
    
    internal UDelegate()
    {
    }

    static UDelegate()
    {
        Wrapper = Type.GetType($"U{typeof(T).FullName}");
        if (Wrapper is null)
        {
            throw new Exception($"Could not find wrapper for {typeof(T).FullName}");
        }
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
    /// Removes a function from the delegate.
    /// Recommend using - operator instead.
    /// </summary>
    public void Remove(T handler)
    {
        InnerDelegate.Remove(handler);
    }
    
    /// <summary>
    /// Checks if the delegate contains a function.
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
    
    public static UDelegate<T> operator +(UDelegate<T> thisDelegate, T handler)
    {
        thisDelegate.InnerDelegate.Add(handler);
        return thisDelegate;
    }

    public static UDelegate<T> operator -(UDelegate<T> thisDelegate, T handler)
    {
        thisDelegate.InnerDelegate.Remove(handler);
        return thisDelegate;
    }
}

public class UMulticastDelegate<T> : UDelegate<T> where T : Delegate
{
    public UMulticastDelegate() : base()
    {
        
    }
};

public class USingleDelegate<T> : UDelegate<T> where T : Delegate
{
    public USingleDelegate() : base()
    {
        
    }
};
