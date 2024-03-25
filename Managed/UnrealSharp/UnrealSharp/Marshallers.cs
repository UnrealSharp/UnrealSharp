using System.Reflection;
using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

public static class MarshalingDelegates<T>
{
    public delegate void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, T obj);
    public delegate T FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner);
    public delegate void DestructInstance(IntPtr nativeBuffer, int arrayIndex);
}

public static class BlittableMarshaller<T>
{ 
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, T obj)
    {
        unsafe
        {
            ToNative(nativeBuffer, arrayIndex, owner, obj, sizeof(T));
        }
    }
    
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, T obj, int size)
    {
        unsafe
        {
            *(T*)(nativeBuffer + arrayIndex * size) = obj;
        }
    }

    public static T FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        unsafe
        {
            return FromNative(nativeBuffer, arrayIndex, owner, sizeof(T));
        }
    }
    
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, int size)
    {
        unsafe
        {
            return *FromNativePtr(nativeBuffer, arrayIndex, owner, size);
        }
    }
    
    public static unsafe T* FromNativePtr(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, int size)
    {
        return (T*)(nativeBuffer + arrayIndex * size);
    }
}

public static class BoolMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, bool obj)
    {
        BlittableMarshaller<NativeBool>.ToNative(nativeBuffer, arrayIndex, owner, obj.ToNativeBool());
    }

    public static bool FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        return BlittableMarshaller<NativeBool>.FromNative(nativeBuffer, arrayIndex, owner).ToManagedBool();
    }
}

public static class ObjectMarshaller<T> where T : UnrealSharpObject
{ 
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, T obj)
    {
        IntPtr uObjectPosition = nativeBuffer + arrayIndex * IntPtr.Size;

        unsafe
        {
            *(IntPtr*) uObjectPosition = obj?.NativeObject ?? IntPtr.Zero;
        }
    }
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        IntPtr uObjectPointer = BlittableMarshaller<IntPtr>.FromNative(nativeBuffer, arrayIndex, owner);
        IntPtr handle = FCSManagerExporter.CallFindManagedObject(uObjectPointer);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
}

public static class StringMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject? owner, string obj)
    {
        unsafe
        {
            var byteCount = (obj.Length + 1) * sizeof(char);
            var stringMemory = Marshal.AllocCoTaskMem(byteCount);
            Marshal.Copy(obj.ToCharArray(), 0, stringMemory, obj.Length);
            Marshal.WriteInt16(stringMemory, obj.Length * sizeof(char), 0);

            UnmanagedArray* unmanagedArray = (UnmanagedArray*) (nativeBuffer + arrayIndex * sizeof(UnmanagedArray));
            unmanagedArray->ArrayNum = obj.Length + 1;
            unmanagedArray->ArrayMax = unmanagedArray->ArrayNum;
            unmanagedArray->Data = stringMemory;
        }
    }

    public static string FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        unsafe
        {
            UnmanagedArray* nativeString = 
                BlittableMarshaller<UnmanagedArray>
                    .FromNativePtr(nativeBuffer, arrayIndex, owner, sizeof(UnmanagedArray));

            return nativeString == null ? default : new string((char*) nativeString->Data);
        }
    }
    
    public static void DestructInstance(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            UnmanagedArray* nativeString = 
                BlittableMarshaller<UnmanagedArray>
                    .FromNativePtr(nativeBuffer, arrayIndex, null, sizeof(UnmanagedArray));
            
            Marshal.FreeCoTaskMem(nativeString->Data);
            nativeString->Data = IntPtr.Zero;
            nativeString->ArrayNum = 0;
            nativeString->ArrayMax = 0;
        }
    }
}
