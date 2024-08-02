using System.Runtime.InteropServices;
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
public struct TWeakObject<T> : IEquatable<TWeakObject<T>> where T : UObject
{
    internal readonly WeakObjectData Data;
    
    /// <summary>
    /// Get the object that this weak object points to.
    /// </summary>
    public T? Object => Get();
    
    public TWeakObject(T obj)
    { 
        FWeakObjectPtrExporter.CallSetObject(ref Data, obj?.NativeObject ?? IntPtr.Zero);
    }
    
    internal TWeakObject(WeakObjectData data)
    {
        Data = data;
    }
    
    internal TWeakObject(UObject targetObject)
    {
        FWeakObjectPtrExporter.CallSetObject(ref Data, targetObject.NativeObject);
    }
    
    public static implicit operator TWeakObject<T>(T obj)
    {
        return new TWeakObject<T>(obj);
    }
    
    private T? Get()
    {
        IntPtr handle = FWeakObjectPtrExporter.CallGetObject(Data);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }

    /// <summary>
    /// Check if the object that this weak object points to is valid.
    /// </summary>
    /// <returns>True if the object is valid, false otherwise.</returns>
    public bool IsValid()
    {
        return FWeakObjectPtrExporter.CallIsValid(Data).ToManagedBool();
    }

    /// <summary>
    /// Check if the object that this weak object points to is stale.
    /// </summary>
    /// <returns>True if the object is stale, false otherwise.</returns>
    public bool IsStale()
    {
        return FWeakObjectPtrExporter.CallIsStale(Data).ToManagedBool();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return IsValid() ? Object.ToString() : "None";
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Data.ObjectIndex;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (obj is TWeakObject<T> other)
        {
            return Equals(other);
        }

        return false;
    }

    /// <inheritdoc />
    public bool Equals(TWeakObject<T> other)
    {
        return FWeakObjectPtrExporter.CallNativeEquals(Data, other.Data).ToManagedBool();
    }
}