using System.Diagnostics.CodeAnalysis;
using UnrealSharp.Attributes;
using UnrealSharp.Core.Marshallers;

namespace UnrealSharp;

[UClass]
public class TMapReadOnly<TKey, TValue> : MapBase<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
{
    /// <inheritdoc />
    public TMapReadOnly(IntPtr mapProperty, IntPtr address,
        MarshallingDelegates<TKey>.FromNative keyFromNative, MarshallingDelegates<TKey>.ToNative keyToNative,
        MarshallingDelegates<TValue>.FromNative valueFromNative, MarshallingDelegates<TValue>.ToNative valueToNative)
        : base(mapProperty, address, keyFromNative, keyToNative, valueFromNative, valueToNative)
    {
    }

    /// <inheritdoc />
    public TValue this[TKey key] => TryGetInternal(key, out var value) ? value : throw new KeyNotFoundException();

    public KeyEnumerator Keys => new(this);
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => new KeyEnumerator(this);
    public ValueCollection Values => new(this);
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => new ValueCollection(this);

    /// <inheritdoc />
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return TryGetInternal(key, out value);
    }
}