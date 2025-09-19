
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp;

public readonly struct TOptional<T>(bool hasValue, T value)
    : IEquatable<TOptional<T>>, IComparable<TOptional<T>>
{
    public readonly bool HasValue => hasValue;
    public readonly T Value => value;

    public static readonly TOptional<T> None = default;
    public static TOptional<T> Some(T v) => new(true, v);

    public bool Equals(TOptional<T> other)
    {
        if (hasValue != other.HasValue) return false;
        if (!hasValue) return true;
        return EqualityComparer<T>.Default.Equals(value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TOptional<T> o) return Equals(o);
        if (!hasValue) return obj is null;
        if (obj is null) return false;
        return obj is T t && EqualityComparer<T>.Default.Equals(value, t);
    }

    public override int GetHashCode()
        => hasValue ? value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(value) : 0;

    public int CompareTo(TOptional<T> other)
    {
        if (hasValue)
        {
            if (!other.HasValue) return 1;
            var a = value;
            var b = other.Value;

            if (a is null) return b is null ? 0 : -1;
            if (b is null) return 1;

            if (a is IComparable<T> ct) return ct.CompareTo(b);
            if (a is IComparable c) return c.CompareTo(b);
            return 0;
        }
        return other.HasValue ? -1 : 0;
    }

    public int CompareTo(T other)
    {
        if (!hasValue) return -1;
        var a = value;

        if (a is null) return other is null ? 0 : -1;
        if (a is IComparable<T> ct) return ct.CompareTo(other);
        if (a is IComparable c) return c.CompareTo(other);
        return 0;
    }

    public static bool operator ==(TOptional<T> left, TOptional<T> right) => left.Equals(right);
    public static bool operator !=(TOptional<T> left, TOptional<T> right) => !left.Equals(right);
    public static bool operator true(TOptional<T> opt) => opt.HasValue;
    public static bool operator false(TOptional<T> opt) => !opt.HasValue;
    public static bool operator !(TOptional<T> opt) => !opt.HasValue;

    public static implicit operator TOptional<T>(T v) => new(true, v);
    public static explicit operator T(TOptional<T> opt) => opt.Value;

    public override string ToString() => hasValue ? value?.ToString() ?? string.Empty : string.Empty;
}

public class OptionalMarshaller<T>(IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
{
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, TOptional<T> obj)
    {
        unsafe
        {
            if (obj)
            {
                var result = FOptionalPropertyExporter.CallMarkSetAndGetInitializedValuePointerToReplace(nativeProperty, nativeBuffer);
                toNative(result, 0, obj.Value);
            }
            else
            {
                FOptionalPropertyExporter.MarkUnset(nativeProperty, nativeBuffer);
            }
        }
    }

    public TOptional<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            if (!FOptionalPropertyExporter.CallIsSet(nativeProperty, nativeBuffer).ToManagedBool()) return TOptional<T>.None;
            var result = FOptionalPropertyExporter.GetValuePointerForRead(nativeProperty, nativeBuffer);
            return fromNative(result, 0);
        }
    }
}