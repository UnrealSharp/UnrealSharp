using UnrealSharp.Attributes;
using UnrealSharp.Core.Marshallers;

namespace UnrealSharp;

[UClass]
public class TMapReadOnly<TKey, TValue> : MapBase<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
{
    /// <inheritdoc />
    public TMapReadOnly(IntPtr mapProperty, IntPtr address,
        MarshallingDelegates<TKey>.FromNative keyFromNative, MarshallingDelegates<TKey>.ToNative keyToNative,
        MarshallingDelegates<TValue>.FromNative valueFromNative, MarshallingDelegates<TValue>.ToNative valueToNative)
        : base(mapProperty, address, keyFromNative, keyToNative, valueFromNative, valueToNative)
    {
    }

    /// <inheritdoc />
    public TValue this[TKey key] => Get(key);

    public KeyEnumerator Keys => new(this);
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => new KeyEnumerator(this);
    public ValueCollection Values => new(this);
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => new ValueCollection(this);

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue? value)
    {
        var index = IndexOf(key);
        if (index >= 0)
        {
            value = GetAt(index).Value;
            return true;
        }
        value = default;
        return false;
    }
}