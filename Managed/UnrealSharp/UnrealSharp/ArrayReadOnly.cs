namespace UnrealSharp;

public class ArrayReadOnly<T> : UnrealArrayBase<T>, IReadOnlyList<T>
{
    [CLSCompliant(false)]
    public ArrayReadOnly(IntPtr nativeUnrealProperty, IntPtr nativeBuffer, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
        : base(nativeUnrealProperty, nativeBuffer, toNative, fromNative)
    {
    }

    /// <inheritdoc />
    public T this[int index] => Get(index);
}

internal class ArrayReadOnlyMarshaller<T>
{
    private readonly IntPtr _nativeProperty;
    private readonly ArrayReadOnly<T>[] _wrappers;
    private readonly MarshallingDelegates<T>.FromNative _innerTypeFromNative;

    public ArrayReadOnlyMarshaller(int length, IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
    {
        _nativeProperty = nativeProperty;
        _wrappers = new ArrayReadOnly<T>[length];
        _innerTypeFromNative = fromNative;
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, ArrayReadOnly<T> obj)
    {
        throw new NotImplementedException("Copying UnrealArrays from managed memory to native memory is unsupported.");
    }

    public ArrayReadOnly<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (_wrappers[arrayIndex] == null)
        {
            unsafe
            {
                _wrappers[arrayIndex] = new ArrayReadOnly<T>(_nativeProperty, nativeBuffer + arrayIndex * sizeof(UnmanagedArray), null, _innerTypeFromNative);
            }
        }
        return _wrappers[arrayIndex];
    }
}