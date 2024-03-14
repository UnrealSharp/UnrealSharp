namespace UnrealSharp;

public static class MarshallingDelegates<T>
{
    public delegate void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, T obj);
    public delegate T FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner);
    public delegate void DestructInstance(IntPtr nativeBuffer, int arrayIndex);
}