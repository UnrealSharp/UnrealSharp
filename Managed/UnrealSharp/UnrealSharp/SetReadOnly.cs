using UnrealSharp.Interop.Properties;

namespace UnrealSharp;

public class TSetReadOnly<T> : TSetBase<T>, IReadOnlySet<T>
{
    public TSetReadOnly(IntPtr nativeProperty, IntPtr address, MarshallingDelegates<T>.FromNative fromNative) : base(nativeProperty, address, fromNative, null)
    {
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        return IsProperSubsetOfInternal(other);
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        return IsProperSupersetOfInternal(other);
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        return IsSubsetOfInternal(other);
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        return IsSupersetOfInternal(other);
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        return OverlapsInternal(other);
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        return SetEqualsInternal(other);
    }
}

public class SetReadOnlyMarshaller<T>
{
    readonly NativeProperty _property;
    readonly TSetReadOnly<T>[] _wrappers;
    readonly MarshallingDelegates<T>.FromNative _elementFromNative;

    public SetReadOnlyMarshaller(int length, IntPtr setProperty,
        MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
    {
        _property = new NativeProperty(setProperty);
        _wrappers = new TSetReadOnly<T>[length];
        _elementFromNative = fromNative;
    }

    public TSetReadOnly<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (_wrappers[arrayIndex] == null)
        {
            _wrappers[arrayIndex] = new TSetReadOnly<T>(_property.Property, _property.ValueAddress(nativeBuffer), _elementFromNative);
        }
        
        return _wrappers[arrayIndex];
    }

    public void ToNative(IntPtr nativeBuffer, IReadOnlyCollection<T> value)
    {
        ToNative(nativeBuffer, 0, IntPtr.Zero, value);
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop, IReadOnlyCollection<T> value)
    {
        throw new NotImplementedException("Read-only TSet cannot write to native memory.");
    }
}