using UnrealSharp.Core.Interop;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp;

public class UnmanagedTypeMarshaller<T> where T : unmanaged
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        unsafe
        {
            ManagedHandleExporter.CallStoreUnmanagedMemory((IntPtr)(&obj), nativeBuffer + arrayIndex * FUnmanagedDataStore.GetNativeDataSize(), sizeof(T));
        }
    }
    
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (nativeBuffer == IntPtr.Zero)
        {
            return default;
        }

        unsafe
        {
            T output = default;
            ManagedHandleExporter.CallLoadUnmanagedMemory(
                nativeBuffer + arrayIndex * FUnmanagedDataStore.GetNativeDataSize(), (IntPtr)(&output), sizeof(T));
            return output;
        }
    }
}