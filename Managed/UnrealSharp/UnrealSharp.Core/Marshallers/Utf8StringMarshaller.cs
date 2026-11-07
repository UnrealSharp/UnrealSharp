using UnrealSharp.Core.Interop;

namespace UnrealSharp.Core.Marshallers;

public static unsafe class Utf8StringMarshaller
{
    /// <summary>Assigns an independent copy to an initialized native FUtf8String.</summary>
    public static void ToNative(IntPtr buffer, int index, FUtf8String value)
    {
        fixed (byte* bytes = value.Bytes)
        {
            Bind_FUtf8String.CallAssign((UnmanagedArray*)buffer + index, bytes, value.Length);
        }
    }

    public static FUtf8String FromNative(IntPtr buffer, int index)
    {
        UnmanagedArray value = ((UnmanagedArray*)buffer)[index];
        return new FUtf8String(new ReadOnlySpan<byte>((void*)value.Data, Math.Max(0, value.ArrayNum - 1)));
    }

    public static void DestructInstance(IntPtr buffer, int index)
    {
        Bind_FUtf8String.CallDestroy((UnmanagedArray*)buffer + index);
    }
}
