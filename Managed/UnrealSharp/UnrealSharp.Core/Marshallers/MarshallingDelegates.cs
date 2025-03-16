namespace UnrealSharp.Core.Marshallers;

public static class MarshallingDelegates<T>
{
    public delegate void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj);
    public delegate T FromNative(IntPtr nativeBuffer, int arrayIndex);
    public delegate void DestructInstance(IntPtr nativeBuffer, int arrayIndex);
}