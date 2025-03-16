namespace UnrealSharp.Core.Marshallers;

public static class StructMarshaller<T> where T : MarshalledStruct<T>
{
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        return T.FromNative(nativeBuffer + arrayIndex * T.GetNativeDataSize());
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        obj.ToNative(nativeBuffer + arrayIndex * T.GetNativeDataSize());
    }
}