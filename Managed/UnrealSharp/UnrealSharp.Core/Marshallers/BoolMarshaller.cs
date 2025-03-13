namespace UnrealSharp.Core.Marshallers;

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