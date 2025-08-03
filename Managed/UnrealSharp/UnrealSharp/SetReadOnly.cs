using UnrealSharp.Core.Marshallers;
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
    private readonly FScriptSetHelper _helper;
    readonly MarshallingDelegates<T>.FromNative _elementFromNative;
    readonly MarshallingDelegates<T>.ToNative _elementToNative;
    private TSetReadOnly<T>? _readonlySetWrapper;

    public SetReadOnlyMarshaller(IntPtr setProperty,
        MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
    {
        _property = new NativeProperty(setProperty);
        _helper = new FScriptSetHelper(_property);
        _elementFromNative = fromNative;
        _elementToNative = toNative;
    }

    public TSetReadOnly<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (_readonlySetWrapper == null)
        {
            _readonlySetWrapper = new TSetReadOnly<T>(_property.Property, _property.ValueAddress(nativeBuffer), _elementFromNative);
        }
        
        return _readonlySetWrapper;
    }

    public void ToNative(IntPtr nativeBuffer, IReadOnlyCollection<T> value)
    {
        ToNative(nativeBuffer, 0, IntPtr.Zero, value);
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop, IReadOnlyCollection<T> value)
    {
        SetMarshaller<T>.ToNativeInternal(nativeBuffer, arrayIndex, value, _helper, _elementToNative);
    }
}