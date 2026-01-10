using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp;

public class TArrayReadOnly<T> : UnrealArrayBase<T>, IReadOnlyList<T>
{
    public TArrayReadOnly(IntPtr nativeUnrealProperty, IntPtr nativeBuffer, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
        : base(nativeUnrealProperty, nativeBuffer, toNative, fromNative)
    {
    }

    /// <inheritdoc />
    public T this[int index] => Get(index);
}

public class ArrayReadOnlyMarshaller<T>(IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
{
    private TArrayReadOnly<T>? _readOnlyWrapper;

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IReadOnlyList<T> obj)
    {
        ToNative(nativeBuffer, obj);
    }

    public void ToNative(IntPtr nativeBuffer, IReadOnlyList<T> obj)
    {
        unsafe
        {
            UnmanagedArray* mirror = (UnmanagedArray*)nativeBuffer;
            if (mirror->ArrayNum == obj.Count)
            {
                for (int i = 0; i < obj.Count; ++i)
                {
                    toNative(mirror->Data, i, obj[i]);
                }
            }
            else
            {
                FArrayPropertyExporter.CallResizeArray(nativeProperty, mirror, obj.Count);
                for (int i = 0; i < obj.Count; ++i)
                {
                    toNative(mirror->Data, i, obj[i]);
                }
            }
        }
    }

    public TArrayReadOnly<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (_readOnlyWrapper == null)
        {
            unsafe
            {
                _readOnlyWrapper = new TArrayReadOnly<T>(nativeProperty, nativeBuffer + arrayIndex * sizeof(UnmanagedArray), toNative, fromNative);
            }
        }
        return _readOnlyWrapper;
    }
}