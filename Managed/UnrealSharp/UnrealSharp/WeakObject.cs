using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct WeakObjectData
{
    public int ObjectIndex;
    public int ObjectSerialNumber;
}

public struct WeakObject<T> : IEquatable<WeakObject<T>> where T : UnrealSharpObject
{
    private readonly WeakObjectData _data;
    public T Object => Get();
    
    public WeakObject(T obj)
    { 
        FWeakObjectPtrExporter.CallSetObject(ref _data, obj?.NativeObject ?? IntPtr.Zero);
    }
    
    internal WeakObject(WeakObjectData data)
    {
        _data = data;
    }
    
    internal WeakObject(CoreUObject.Object targetObject)
    {
        FWeakObjectPtrExporter.CallSetObject(ref _data, targetObject.NativeObject);
    }
    
    public static implicit operator WeakObject<T>(T obj)
    {
        return new WeakObject<T>(obj);
    }

    private T Get()
    {
        IntPtr handle = FWeakObjectPtrExporter.CallGetObject(_data);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }

    public bool IsValid()
    {
        return FWeakObjectPtrExporter.CallIsValid(_data).ToManagedBool();
    }

    public bool IsStale()
    {
        return FWeakObjectPtrExporter.CallIsStale(_data).ToManagedBool();
    }
    
    public override string ToString()
    {
        return IsValid() ? Object.ToString() : "None";
    }

    public override int GetHashCode()
    {
        return _data.ObjectIndex;
    }

    public override bool Equals(object obj)
    {
        if (obj is WeakObject<T> other)
        {
            return Equals(other);
        }

        return false;
    }

    public bool Equals(WeakObject<T> other)
    {
        return FWeakObjectPtrExporter.CallNativeEquals(_data, other._data).ToManagedBool();
    }
}