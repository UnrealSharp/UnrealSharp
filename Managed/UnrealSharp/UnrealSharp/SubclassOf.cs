using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public readonly struct SubclassOf<T>
{
    internal IntPtr NativeClass { get; }
    private Type ManagedType { get; }
    
    public bool Valid => IsChildOf(typeof(T));
    
    public SubclassOf()
    {
        unsafe
        {
            Type type = typeof(T);
            NativeClass = UCoreUObjectExporter.GetNativeClassFromName(type.Name);
            ManagedType = type;
        }
    }
    
    public SubclassOf(Type classType)
    {
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
        NativeClass = nativeClass;
        IntPtr handle = UClassExporter.CallGetDefaultFromInstance(nativeClass);
        ManagedType = GcHandleUtilities.GetObjectFromHandlePtr<T>(handle).GetType();
    }
    
    public T GetDefaultObject()
    {
        IntPtr handle = UClassExporter.CallGetDefaultFromString(ManagedType.Name);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public override string ToString()
    {
        return Valid ? ManagedType.Name : "null";
    }
    
    public SubclassOf<T> As<T>()
    {
        if (!IsChildOf(typeof(T)))
        {
            throw new InvalidOperationException();
        }

        return new SubclassOf<T>(NativeClass);
    }

    public bool IsChildOf(Type type)
    {
        return ManagedType != null && (ManagedType == type || ManagedType.IsSubclassOf(type));
    }
    
    public bool IsParentOf(Type type)
    {
        return ManagedType != null && ManagedType.IsAssignableFrom(type);
    }
    
    public static implicit operator SubclassOf<T>(Type inClass)
    {
        return new SubclassOf<T>(inClass);
    }
    
    public override bool Equals(object obj)
    {
        return obj is SubclassOf<T> other && NativeClass == other.NativeClass;
    }
    
    public override int GetHashCode()
    {
        return NativeClass.GetHashCode();
    }
}

public static class SubclassOfMarshaller<T>
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, SubclassOf<T> obj)
    {
        BlittableMarshaller<IntPtr>.ToNative(nativeBuffer, arrayIndex, owner, obj.NativeClass);
    }

    public static SubclassOf<T> FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        IntPtr nativeClassPointer = BlittableMarshaller<IntPtr>.FromNative(nativeBuffer, arrayIndex, owner);
        return new SubclassOf<T>(nativeClassPointer);
    }
}