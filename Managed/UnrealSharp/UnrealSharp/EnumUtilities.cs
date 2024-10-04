namespace UnrealSharp;

public static class EnumMarshaller<T> where T : Enum
{
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        byte value = BlittableMarshaller<byte>.FromNative(nativeBuffer, arrayIndex);
        return (T) Enum.ToObject(typeof(T), value);
    }
    
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        byte value = Convert.ToByte(obj);
        BlittableMarshaller<byte>.ToNative(nativeBuffer, arrayIndex, value);
    }
}