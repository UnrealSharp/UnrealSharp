using System.Runtime.CompilerServices;

namespace UnrealSharp.Core.Marshallers;

public static class BlittableMarshaller<T> where T : unmanaged, allows ref struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        unsafe
        {
            *(T*)(nativeBuffer + arrayIndex * sizeof(T)) = obj;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            return *(T*)(nativeBuffer + arrayIndex * sizeof(T));
        }
    }
}
