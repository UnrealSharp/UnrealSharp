using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

public static class MarshallingDelegates<T>
{
    public delegate void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj);
    public delegate T FromNative(IntPtr nativeBuffer, int arrayIndex);
    public delegate void DestructInstance(IntPtr nativeBuffer, int arrayIndex);
}

public static class BlittableMarshaller<T>
{ 
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        unsafe
        {
            ToNative(nativeBuffer, arrayIndex, obj, sizeof(T));
        }
    }
    
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj, int size)
    {
        unsafe
        {
            *(T*)(nativeBuffer + arrayIndex * size) = obj;
        }
    }
    
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            return FromNative(nativeBuffer, arrayIndex, sizeof(T));
        }
    }
    
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex, int size)
    {
        unsafe
        {
            return *(T*)(nativeBuffer + arrayIndex * size);
        }
    }
}

public static class BoolMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, bool obj)
    {
        BlittableMarshaller<NativeBool>.ToNative(nativeBuffer, arrayIndex, obj.ToNativeBool());
    }
    
    public static bool FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        return BlittableMarshaller<NativeBool>.FromNative(nativeBuffer, arrayIndex).ToManagedBool();
    }
}

public static class ObjectMarshaller<T> where T : UnrealSharpObject
{ 
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        IntPtr uObjectPosition = nativeBuffer + arrayIndex * IntPtr.Size;

        unsafe
        {
            *(IntPtr*) uObjectPosition = obj?.NativeObject ?? IntPtr.Zero;
        }
    }
    
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        IntPtr uObjectPointer = BlittableMarshaller<IntPtr>.FromNative(nativeBuffer, arrayIndex);
        IntPtr handle = FCSManagerExporter.CallFindManagedObject(uObjectPointer);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
}

public static class StringMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, string obj)
    {
        unsafe
        {
            IntPtr ustring = nativeBuffer + arrayIndex * sizeof(UnmanagedArray);
            fixed (char* stringPtr = obj)
            {
                FStringExporter.CallMarshalToNativeString(ustring, stringPtr);
            }
        }
    }
    
    public static string FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            UnmanagedArray nativeString = BlittableMarshaller<UnmanagedArray>.FromNative(nativeBuffer, arrayIndex);
            return nativeString.Data == IntPtr.Zero ? string.Empty : new string((char*) nativeString.Data);
        }
    }
    
    public static void DestructInstance(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            UnmanagedArray* ustring = (UnmanagedArray*) (nativeBuffer + arrayIndex * sizeof(UnmanagedArray));
            ustring->Destroy();
        }
    }
}
