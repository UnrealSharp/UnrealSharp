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

    // <summary>
    /// Checks if the delegate is bound to any UObject.
    /// </summary>
    public virtual bool IsBound => throw new NotImplementedException();
}

public class DelegateMarshaller<TWrapperDelegate, TDelegate> where TWrapperDelegate : TDelegateBase<TDelegate>, new() where TDelegate : Delegate
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

public class TDelegateBase<T> where T : Delegate
{
    private static readonly Type Wrapper;
    public DelegateBase<T> InnerDelegate;

    internal TDelegateBase()
    {
        InnerDelegate = (DelegateBase<T>) Activator.CreateInstance(Wrapper);
    }

    static TDelegateBase()
    {
        string wrapperName = $"U{typeof(T).Name}";
        string fullName = $"{typeof(T).Namespace}.{wrapperName}";
        
        Wrapper = Type.GetType(fullName);
        if (Wrapper == null)
        {
            throw new TypeLoadException($"Could not find wrapper type '{fullName}' for '{typeof(T).FullName}'");
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
    public TMulticastDelegate() : base()
    {
        
    }
    
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

public class TSingleDelegate<T> : TDelegateBase<T> where T : Delegate
{
    public TSingleDelegate() : base()
    {
        
    }
    
    public static TSingleDelegate<T> operator +(TSingleDelegate<T> thisDelegate, T handler)
    {
        thisDelegate.InnerDelegate.Add(handler);
        return thisDelegate;
    }

    public static TSingleDelegate<T> operator -(TSingleDelegate<T> thisDelegate, T handler)
    {
        thisDelegate.InnerDelegate.Remove(handler);
        return thisDelegate;
    }
};
