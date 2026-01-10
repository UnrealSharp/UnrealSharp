
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp;

public readonly struct TOptional<T>
    : IEquatable<TOptional<T>>, IComparable<TOptional<T>> where T : notnull
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T? _value;
    
    [MemberNotNullWhen(true, nameof(_value))]
    public bool HasValue { get; }
    public bool IsEmpty => !HasValue;
    
    public T Value => OrElseThrow();

    private TOptional(T? value)
    {
        HasValue = value is not null;
        _value = value;
    }

    public static readonly TOptional<T> None = default;
    public static TOptional<T> Some(T v) => new(v);

    public bool TryGet([NotNullWhen(true)] out T? value)
    {
        if (HasValue)
        {
            value = _value;
            return true;
        }
        
        value = default;
        return false;
    }

    public T OrElse(T other) => HasValue ? _value : other;
    
    public T OrElseGet(Func<T> other) => HasValue ? _value : other();
    
    public T OrElseThrow() => HasValue ? _value : throw new InvalidOperationException("Value is not set");
    
    public T OrElseThrow(Func<Exception> exception) => HasValue ? _value : throw exception();
    
    public TOptional<T> Or(Func<TOptional<T>> other) => HasValue ? this : other();

    public TOptional<TOther> Select<TOther>(Func<T, TOther?> selector) where TOther : notnull
    {
        return HasValue ? new TOptional<TOther>(selector(_value)) : TOptional<TOther>.None;
    }

    public TOptional<TOther> Select<TOther>(Func<T, TOther?> selector) where TOther : struct
    {
        return HasValue ? selector(_value).ToOptional() : TOptional<TOther>.None;   
    }

    public TOptional<TOther> SelectMany<TOther>(Func<T, TOptional<TOther>> selector) where TOther : notnull
    {
        return HasValue ? selector(_value) : TOptional<TOther>.None;
    }

    public TOptional<T> Where(Func<T, bool> predicate)
    {
        return HasValue && predicate(_value) ? this : None;
    }

    public void IfSome(Action<T> action)
    {
        if (HasValue) action(_value);
    }

    public void IfNone(Action action)
    {
        if (!HasValue) action();
    }

    public void Match(Action<T> some, Action none)
    {
        if (HasValue)
            some(_value);
        else
            none();
    }

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        return HasValue ? some(_value) : none();
    }

    public IEnumerable<T> AsEnumerable()
    {
        return HasValue ? [_value] : [];
    }

    public bool Equals(TOptional<T> other)
    {
        if (HasValue != other.HasValue) return false;
        if (!HasValue) return true;
        return EqualityComparer<T>.Default.Equals(_value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TOptional<T> o) return Equals(o);
        if (!HasValue) return obj is null;
        if (obj is null) return false;
        return obj is T t && EqualityComparer<T>.Default.Equals(_value, t);
    }

    public override int GetHashCode()
        => HasValue ? _value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(_value) : 0;

    public int CompareTo(TOptional<T> other)
    {
        if (HasValue)
        {
            if (!other.HasValue) return 1;
            var a = _value;
            var b = other._value;

            if (a is null) return b is null ? 0 : -1;
            if (b is null) return 1;

            if (a is IComparable<T> ct) return ct.CompareTo(b);
            if (a is IComparable c) return c.CompareTo(b);
            return 0;
        }
        return other.HasValue ? -1 : 0;
    }

    public int CompareTo(T? other)
    {
        if (!HasValue) return -1;
        var a = _value;

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

    public static implicit operator TOptional<T>(T? v) => new(v);
    public static explicit operator T(TOptional<T> opt) => opt.Value;

    public override string ToString() => HasValue ? _value?.ToString() ?? string.Empty : string.Empty;
}

public static class OptionalExtensions
{
    public static TOptional<T> ToOptional<T>(this T? value) where T : struct
    {
        return value.HasValue ? TOptional<T>.Some(value.Value) : TOptional<T>.None;
    }
    
    public static T? ToNullable<T>(this TOptional<T> opt) where T : struct
    {
        return opt.TryGet(out var value) ? value : null;
    }
}

public class OptionalMarshaller<T>(IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative) where T : notnull
{
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, TOptional<T> obj)
    {
        if (obj.HasValue)
        {
            IntPtr result = FOptionalPropertyExporter.CallMarkSetAndGetInitializedValuePointerToReplace(nativeProperty, nativeBuffer);
            toNative(result, 0, obj.Value);
        }
        else
        {
            FOptionalPropertyExporter.CallMarkUnset(nativeProperty, nativeBuffer);
        }
    }

    public TOptional<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (!FOptionalPropertyExporter.CallIsSet(nativeProperty, nativeBuffer).ToManagedBool()) return TOptional<T>.None;
        var result = FOptionalPropertyExporter.CallGetValuePointerForRead(nativeProperty, nativeBuffer);
        return fromNative(result, 0);
    }

    public void DestructInstance(IntPtr nativeBuffer, int arrayIndex)
    {
        FOptionalPropertyExporter.CallDestructInstance(nativeProperty, nativeBuffer);
    }
}