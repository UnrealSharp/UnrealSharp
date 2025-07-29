namespace UnrealSharp;

public interface MarshalledStruct<Self> where Self : MarshalledStruct<Self>, allows ref struct
{
    public static abstract IntPtr GetNativeClassPtr();

    public static abstract int GetNativeDataSize();

    public static abstract Self FromNative(IntPtr buffer);

    public void ToNative(IntPtr buffer);
}
