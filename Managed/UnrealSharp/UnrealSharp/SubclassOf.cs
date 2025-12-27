using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Represents a subclass of a specific class.
/// </summary>
/// <typeparam name="T">The base class that the subclass must inherit from.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public struct TSubclassOf<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal IntPtr NativeClass;
    
    /// <summary>
    /// Check if the class is valid.
    /// </summary>
    public bool IsValid => IsChildOf(typeof(T));
    
    public TSubclassOf() : this(typeof(T))
    {
    }

    public TSubclassOf(UClass classType) : this(classType.NativeObject) { }

    public TSubclassOf(Type classType)
    {
        try 
        {
            if (classType == null)
            {
                throw new ArgumentNullException(nameof(classType));
            }
            
            if (classType == typeof(T) || classType.IsSubclassOf(typeof(T)) || typeof(T).IsAssignableFrom(classType))
            {
                string typeName = classType.GetEngineName();
                NativeClass = UCoreUObjectExporter.CallGetType(classType.GetAssemblyName(), classType.Namespace, typeName);
                
                #if !WITH_EDITOR
                IntPtr skeletonClass = UCoreUObjectExporter.CallGetGeneratedClassFromSkeleton(NativeClass);
                if (skeletonClass != IntPtr.Zero)
                {
                    NativeClass = skeletonClass;
                }
                #endif
            }
            else
            {
                throw new ArgumentException($"{classType.Name} is not a subclass of {nameof(T)}.");
            }
        }
        catch (Exception exception)
        {
            LogUnrealSharpCore.Log("Failed to create TSubclassOf instance: " + exception.Message);
        }
    }
    
    internal TSubclassOf(IntPtr nativeClass)
    {
        NativeClass = nativeClass;
    }
    
    /// <summary>
    /// Get the default object of the class.
    /// </summary>
    /// <returns>The default object of the class.</returns>
    public T DefaultObject 
    {
        get
        {
            IntPtr handle = UClassExporter.CallGetDefaultFromInstance(NativeClass);
            return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
        }
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
            throw new InvalidOperationException($"Cannot cast to {typeof(TChildClass).Name}. The class is not a subclass of {typeof(TChildClass).Name}.");
        }

        return new TSubclassOf<TChildClass>(NativeClass);
    }
    
    /// <summary>
    /// Cast the class to a subclass of the specified type.
    /// </summary>
    /// <typeparam name="TChildClass">The type to cast the class to.</typeparam>
    /// <returns> The subclass of the specified type.</returns>
    public TSubclassOf<TChildClass> Cast<TChildClass>()
    {
        return As<TChildClass>();
    }

    /// <summary>
    /// Check if the class is a subclass of the specified type.
    /// </summary>
    /// <param name="type">The type to check against.</param>
    /// <returns></returns>
    public bool IsChildOf(Type type)
    {
        IntPtr nativeClass = type.TryGetNativeClass();
        
        if (nativeClass == IntPtr.Zero)
        {
            LogUnrealSharpCore.Log($"Failed to get native class for type {type.FullName}. Is it a UClass?");
            return false;
        }
        
        return UClassExporter.CallIsChildOf(NativeClass, nativeClass).ToManagedBool();
    }
    
    /// <summary>
    /// Check if the class is a parent of the specified type.
    /// </summary>
    /// <param name="type">The type to check against.</param>
    /// <returns> True if the class is a parent of the specified type, false otherwise. </returns>
    public bool IsParentOf(Type type)
    {
        IntPtr nativeClass = type.TryGetNativeClass();
        
        if (nativeClass == IntPtr.Zero)
        {
            LogUnrealSharpCore.Log($"Failed to get native class for type {type.FullName}. Is it a UClass?");
            return false;
        }
        
        return UClassExporter.CallIsChildOf(nativeClass, NativeClass).ToManagedBool();
    }
    
    public static implicit operator TSubclassOf<T>(Type inClass)
    {
        return new TSubclassOf<T>(inClass);
    }
    
    public static implicit operator TSubclassOf<T>(UClass inClass)
    {
        return new TSubclassOf<T>(inClass);
    }
    
    public static implicit operator UClass(TSubclassOf<T> subclass)
    {
        IntPtr handle = FCSManagerExporter.CallFindManagedObject(subclass.NativeClass);
        return GCHandleUtilities.GetObjectFromHandlePtr<UClass>(handle)!;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
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
        if (!IsValid)
        {
            return "None";
        }
        
        UObjectExporter.CallNativeGetName(NativeClass, out FName className);
        return className.ToString();
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
    
    public static TSubclassOf<T> FromNative(IntPtr nativeBuffer)
    {
        return new TSubclassOf<T>(nativeBuffer);
    }
}