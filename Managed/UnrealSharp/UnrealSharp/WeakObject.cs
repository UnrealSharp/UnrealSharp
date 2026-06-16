using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct WeakObjectData
{
    public int ObjectIndex;
    public int ObjectSerialNumber;
}

/// <summary>
/// A weak reference to an Unreal Engine UObject.
/// </summary>
/// <typeparam name="T">The type of object that this weak object points to.</typeparam>
public readonly struct TWeakObjectPtr<T> : IEquatable<TWeakObjectPtr<T>> where T : UObject
{
    internal readonly WeakObjectData Data;
    
    /// <summary>
    /// Get the object that this weak object points to.
    /// </summary>
    public T? Object => Get();
    
    public TWeakObjectPtr(T obj)
    { 
        Bind_FWeakObjectPtr.CallSetObject(ref Data, obj?.NativeObject ?? IntPtr.Zero);
    }

    internal TWeakObjectPtr(IntPtr nativePtr)
    {
        Bind_FWeakObjectPtr.CallSetObject(ref Data, nativePtr);
    }
    
    internal TWeakObjectPtr(WeakObjectData data)
    {
        Data = data;
    }
    
    internal TWeakObjectPtr(UObject targetObject)
    {
        Bind_FWeakObjectPtr.CallSetObject(ref Data, targetObject.NativeObject);
    }
    
    public static implicit operator TWeakObjectPtr<T>(T obj)
    {
        return new TWeakObjectPtr<T>(obj);
    }
    
    private T? Get()
    {
        IntPtr handle = Bind_FWeakObjectPtr.CallGetObject(Data);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }

    /// <summary>
    /// Check if the object that this weak object points to is valid.
    /// </summary>
    /// <returns>True if the object is valid, false otherwise.</returns>
    public bool IsValid => Bind_FWeakObjectPtr.CallIsValid(Data).ToManagedBool();

    /// <summary>
    /// Check if the object that this weak object points to is stale.
    /// </summary>
    /// <returns>True if the object is stale, false otherwise.</returns>
    public bool IsStale()
    {
        return Bind_FWeakObjectPtr.CallIsStale(Data).ToManagedBool();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return IsValid ? Object!.ToString() : "None";
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Data.ObjectIndex;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is TWeakObjectPtr<T> other)
        {
            return Equals(other);
        }

        return false;
    }

    /// <inheritdoc />
    public bool Equals(TWeakObjectPtr<T> other)
    {
        return Bind_FWeakObjectPtr.CallNativeEquals(Data, other.Data).ToManagedBool();
    }
}