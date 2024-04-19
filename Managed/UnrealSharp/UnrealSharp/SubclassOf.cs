using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Represents a subclass of a specific class.
/// </summary>
/// <typeparam name="T">The base class that the subclass must inherit from.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly struct SubclassOf<T> 
{
    internal IntPtr NativeClass { get; }
    private Type ManagedType { get; }
    
    /// <summary>
    /// Check if the class is valid.
    /// </summary>
    public bool Valid => IsChildOf(typeof(T));
    
    public SubclassOf()
    {
        Type type = typeof(T);
        NativeClass = UCoreUObjectExporter.CallGetNativeClassFromName(type.Name);
        ManagedType = type;
    }
    
    public SubclassOf(Type classType)
    {
        if (classType == null)
        {
            throw new ArgumentNullException(nameof(classType));
        }
        
        if (classType == typeof(T) || classType.IsSubclassOf(typeof(T)))
        {
            NativeClass = UCoreUObjectExporter.CallGetNativeClassFromName(classType.Name);
            ManagedType = classType;
        }
        else
        {
            throw new ArgumentException($"{classType.Name} is not a subclass of {nameof(T)}.");
        }
    }
    
    public SubclassOf(IntPtr nativeClass)
    {
        if (nativeClass == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(nativeClass));
        }
        
        NativeClass = nativeClass;
        IntPtr handle = UClassExporter.CallGetDefaultFromInstance(nativeClass);
        ManagedType = GcHandleUtilities.GetObjectFromHandlePtr(handle).GetType();
    }
    
    /// <summary>
    /// Get the default object of the class.
    /// </summary>
    /// <returns>The default object of the class.</returns>
    public T GetDefaultObject()
    {
        IntPtr handle = UClassExporter.CallGetDefaultFromInstance(NativeClass);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    /// <summary>
    /// Cast the class to a subclass of the specified type.
    /// </summary>
    /// <typeparam name="TChildClass">The type to cast the class to.</typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown if the class is not a subclass of the specified type.</exception>
    public SubclassOf<TChildClass> As<TChildClass>()
    {
        if (!IsChildOf(typeof(TChildClass)))
        {
            throw new InvalidOperationException();
        }

        return new SubclassOf<TChildClass>(NativeClass);
    }

    /// <summary>
    /// Check if the class is a subclass of the specified type.
    /// </summary>
    /// <param name="type">The type to check against.</param>
    /// <returns></returns>
    public bool IsChildOf(Type type)
    {
        return ManagedType != null && (ManagedType == type || ManagedType.IsSubclassOf(type));
    }
    
    /// <summary>
    /// Check if the class is a parent of the specified type.
    /// </summary>
    /// <param name="type">The type to check against.</param>
    /// <returns> True if the class is a parent of the specified type, false otherwise. </returns>
    public bool IsParentOf(Type type)
    {
        return ManagedType != null && ManagedType.IsAssignableFrom(type);
    }
    
    public static implicit operator SubclassOf<T>(Type inClass)
    {
        return new SubclassOf<T>(inClass);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is SubclassOf<T> other && NativeClass == other.NativeClass;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return NativeClass.GetHashCode();
    }

    public static bool operator ==(SubclassOf<T> left, SubclassOf<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SubclassOf<T> left, SubclassOf<T> right)
    {
        return !(left == right);
    }
    
    public override string ToString()
    {
        return Valid ? ManagedType.Name : "null";
    }
}

public static class SubclassOfMarshaller<T>
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, SubclassOf<T> obj)
    {
        BlittableMarshaller<IntPtr>.ToNative(nativeBuffer, arrayIndex, obj.NativeClass);
    }

    public static SubclassOf<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        IntPtr nativeClassPointer = BlittableMarshaller<IntPtr>.FromNative(nativeBuffer, arrayIndex);
        return new SubclassOf<T>(nativeClassPointer);
    }
}