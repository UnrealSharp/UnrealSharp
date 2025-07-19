using UnrealSharp.Attributes;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;

namespace UnrealSharp;

[Binding]
public class TMap<TKey, TValue> : MapBase<TKey, TValue>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
{
    public TMap(IntPtr mapProperty, IntPtr address,
        MarshallingDelegates<TKey>.FromNative keyFromNative, MarshallingDelegates<TKey>.ToNative keyToNative,
        MarshallingDelegates<TValue>.FromNative valueFromNative, MarshallingDelegates<TValue>.ToNative valueToNative)
        : base(mapProperty, address, keyFromNative, keyToNative, valueFromNative, valueToNative)
    {
    }

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get => Get(key);
        set => AddInternal(key, value);
    }

    public bool IsReadOnly => false;

    public KeyEnumerator Keys => new(this);
    
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => new KeyEnumerator(this);
    
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => new KeyEnumerator(this);

    public ValueCollection Values => new(this);
    
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => new ValueCollection(this);

    ICollection<TValue> IDictionary<TKey, TValue>.Values => new ValueCollection(this);

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        // An exception WONT be thrown if it already exists, it will set the key to the new value
        AddInternal(item.Key, item.Value);
    }

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        // An exception WONT be thrown if it already exists, it will set the key to the new value
        AddInternal(key, value);
    }
    
    public void Clear()
    {
        ClearInternal();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return ContainsKey(item.Key);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        int maxIndex = GetMaxIndex();
        int index = arrayIndex;
        for (int i = 0; i < maxIndex; ++i)
        {
            if (IsValidIndex(i))
            {
                array[index++] = GetAt(i);
            }
        }
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return RemoveInternal(item.Key);
    }

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        return RemoveInternal(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
    {
        return TryGetInternal(key, out value);
    }
}