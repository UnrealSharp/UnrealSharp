namespace UnrealSharp.Core.Marshallers;

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