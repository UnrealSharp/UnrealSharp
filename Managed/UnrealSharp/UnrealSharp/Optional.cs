using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp;

public static class Optional
{
    /// <summary>
    /// Construct an OptionalType with a valid value.
    /// </summary>
    /// <param name="value">The underlying value</param>
    /// <typeparam name="T">The type of optional</typeparam>
    /// <returns>The created optional</returns>
    /// <exception cref="ArgumentNullException">If the passed in argument is null</exception>
    public static TOptional<T> Of<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new TOptional<T>(value);
    }

    /// <summary>
    /// Construct an OptionalType with a valid value.
    /// </summary>
    /// <param name="value">The underlying value</param>
    /// <typeparam name="T">The type of optional</typeparam>
    /// <returns>The created optional</returns>
    /// <exception cref="ArgumentNullException">If the passed in argument is null</exception>
    public static TOptional<T> Of<T>(T? value) where T : struct
    {
        if (!value.HasValue)
        {
            throw new ArgumentNullException(nameof(value));
        }
        
        return new TOptional<T>(value.Value);   
    }

    /// <summary>
    /// Construct an OptionalType with a nullable value, returning empty is the value is null.
    /// </summary>
    /// <param name="value">The underlying value</param>
    /// <typeparam name="T">The type of optional</typeparam>
    /// <returns>The created optional</returns>
    public static TOptional<T> OfNullable<T>(T? value) where T : class
    {
        return value != null ? new TOptional<T>(value) : Empty<T>();
    }

    /// <summary>
    /// Construct an OptionalType with a nullable value, returning empty is the value is null.
    /// </summary>
    /// <param name="value">The underlying value</param>
    /// <typeparam name="T">The type of optional</typeparam>
    /// <returns>The created optional</returns>
    public static TOptional<T> OfNullable<T>(T? value) where T : struct
    {
        return value.HasValue ? new TOptional<T>(value.Value) : Empty<T>();
    }
    
    /// <summary>
    /// Construct an OptionalType with no value; i.e. unset
    /// </summary>
    /// <typeparam name="T">The type of optional</typeparam>
    /// <returns></returns>
    public static TOptional<T> Empty<T>()
    {
        return new TOptional<T>();
    }
}

/// <summary>
/// Maps to the TOptional type in the engine. Represents the presence or absense of a value.
/// </summary>
/// <remarks>
/// Currently we are rolling our own version of this type, but it may make more sense to just pull in
/// <see href="https://github.com/louthy/language-ext/tree/main">LanguageExt</see> instead as that
/// library has a much fuller Option monad that we could use instead.
/// </remarks>
/// <typeparam name="T">The type of the value.</typeparam>
[Binding]
public readonly struct TOptional<T> : IEquatable<TOptional<T>>, IEnumerable<T>
{
    private readonly T? _value;
    
    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSet { get; }
    
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsEmpty => !IsSet;
    
    /// <summary>
    /// Construct an OptionalType with no value; i.e. unset
    /// </summary>
    public TOptional()
    {
        _value = default;
        IsSet = false;
    }

    internal TOptional(T value)
    {
        _value = value;
        IsSet = true;
    }

    public static explicit operator T(TOptional<T> optional)
    {
        return optional.Get();
    }

    public static implicit operator TOptional<T>(T? value)
    {
        return value is not null ? new TOptional<T>(value) : Optional.Empty<T>();
    }


    public T Get()
    {
        if (!IsSet)
        {
            throw new InvalidOperationException("Optional value is not set");
        }
        
        return _value;
    }

    public T Get(T defaultValue)
    {
        return IsSet ? _value : defaultValue;
    }

    public T Get(Func<T> defaultValue)
    {
        return IsSet ? _value : defaultValue();
    }

    public T? GetOrDefault()
    {
        return IsSet ? _value : default;
    }

    public void Match(Action<T> onValue, Action onEmpty)
    {
        if (IsSet)
        {
            onValue(_value);
        }
        else
        {
            onEmpty();
        }
    }

    public TReturn Match<TReturn>(Func<T, TReturn> onValue, Func<TReturn> onEmpty)
    {
        return IsSet ? onValue(_value) : onEmpty();
    }

    public void IfPresent(Action<T> action)
    {
        if (IsSet)
        {
            action(_value);
        }
    }

    public void IfEmpty(Action action)
    {
        if (!IsSet)
        {
            action();
        }
    }
    
    public TOptional<TReturn> Select<TReturn>(Func<T, TReturn?> mapper)
    {
        if (!IsSet) return Optional.Empty<TReturn>();
        
        var result = mapper(_value);
        return result is not null ? new TOptional<TReturn>(result) : Optional.Empty<TReturn>();

    }

    public TOptional<TReturn> Select<TReturn>(Func<T, TReturn?> mapper) where TReturn : struct
    {
        return IsSet ? Optional.OfNullable(mapper(_value)) : Optional.Empty<TReturn>();
    }


    public TOptional<TReturn> SelectMany<TReturn>(Func<T, TOptional<TReturn>> mapper)
    {
        return IsSet ? mapper(_value) : Optional.Empty<TReturn>();
    }
    
    public TOptional<T> Where(Func<T, bool> predicate)
    {
        return IsSet && predicate(_value) ? this : Optional.Empty<T>();
    }
    
    public bool Equals(TOptional<T> other)
    {
        if (IsSet != other.IsSet) return false;

        return !IsSet || _value.Equals(other._value);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is TOptional<T> other)
        {
            return Equals(other);
        }

        return false;
    }

    public static bool operator ==(TOptional<T> left, TOptional<T> right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(TOptional<T> left, TOptional<T> right)
    {
        return !left.Equals(right);
    }
    
    
    public override int GetHashCode()
    {
        return IsSet ? _value.GetHashCode() : 0;
    }

    public IEnumerable<T> AsEnumerable()
    {
        return IsSet ? [_value] : [];   
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        return AsEnumerable().GetEnumerator();
    }
}

public class OptionalMarshaller<T>(IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
{
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, TOptional<T> obj)
    {
        unsafe
        {
            if (obj.IsSet)
            {
                var result = FOptionalPropertyExporter.CallMarkSetAndGetInitializedValuePointerToReplace(nativeProperty, nativeBuffer);
                toNative(result, 0, obj.GetOrDefault()!);
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
            if (!FOptionalPropertyExporter.CallIsSet(nativeProperty, nativeBuffer).ToManagedBool()) return Optional.Empty<T>();
            var result = FOptionalPropertyExporter.GetValuePointerForRead(nativeProperty, nativeBuffer);
            return Optional.Of(fromNative(result, 0));
        }
    }
}