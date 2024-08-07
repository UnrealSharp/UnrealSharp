using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Represents a subclass of a specific class.
/// </summary>
/// <typeparam name="T">The base class that the subclass must inherit from.</typeparam>
[StructLayout(LayoutKind.Sequential), Binding]
public readonly struct TSubclassOf<T> 
{
    internal IntPtr NativeClass { get; }
    private Type ManagedType { get; }
    
    /// <summary>
    /// Check if the class is valid.
    /// </summary>
    public bool Valid => IsChildOf(typeof(T));
    
    public TSubclassOf() : this(typeof(T))
    {
    }
    
    public TSubclassOf(Type classType)
    {
        if (classType == null)
        {
            throw new ArgumentNullException(nameof(classType));
        }
        
        if (classType == typeof(T) || classType.IsSubclassOf(typeof(T)) || typeof(T).IsAssignableFrom(classType))
        {
            string typeName = classType.GetEngineName();
            NativeClass = UCoreUObjectExporter.CallGetNativeClassFromName(typeName);
            ManagedType = classType;
            
            if (NativeClass == IntPtr.Zero)
            {
                throw new ArgumentException($"Class {classType.Name} not found.");
            }
        }
        else
        {
            throw new ArgumentException($"{classType.Name} is not a subclass of {nameof(T)}.");
        }
    }
    
    internal TSubclassOf(IntPtr nativeClass)
    {
        if (nativeClass == IntPtr.Zero)
        {
            return;
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
    public TSubclassOf<TChildClass> As<TChildClass>()
    {
        if (!IsChildOf(typeof(TChildClass)))
        {
            throw new InvalidOperationException();
        }

        return new TSubclassOf<TChildClass>(NativeClass);
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
    
    public static implicit operator TSubclassOf<T>(Type inClass)
    {
        return new TSubclassOf<T>(inClass);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is TSubclassOf<T> other && NativeClass == other.NativeClass;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return NativeClass.GetHashCode();
    }

    public static bool operator ==(TSubclassOf<T> left, TSubclassOf<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TSubclassOf<T> left, TSubclassOf<T> right)
    {
        return !(left == right);
    }
    
    public override string ToString()
    {
        return Valid ? UObjectExporter.CallNativeGetName(NativeClass).ToString() : "null";
    }
}

public static class SubclassOfMarshaller<T>
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, TSubclassOf<T> obj)
    {
        BlittableMarshaller<IntPtr>.ToNative(nativeBuffer, arrayIndex, obj.NativeClass);
    }

    public static TSubclassOf<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        IntPtr nativeClassPointer = BlittableMarshaller<IntPtr>.FromNative(nativeBuffer, arrayIndex);
        return new TSubclassOf<T>(nativeClassPointer);
    }
}