using System.Collections;
using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

public unsafe class MapBase<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    public int Count => Map->Count;
    
    private readonly MarshallingDelegates<TKey>.FromNative KeyFromNative;
    private readonly MarshallingDelegates<TKey>.ToNative? KeyToNative;
    private readonly MarshallingDelegates<TValue>.FromNative ValueFromNative;
    private readonly MarshallingDelegates<TValue>.ToNative? ValueToNative;
    
    internal IntPtr NativeProperty;
    internal IntPtr Address => (IntPtr)Map;
    internal FScriptMap* Map;
    
    internal ScriptMapHelper Helper;

    internal readonly NativeProperty KeyProperty;
    internal readonly NativeProperty ValueProperty;

    public MapBase(IntPtr mapProperty, IntPtr address,
        MarshallingDelegates<TKey>.FromNative keyFromNative, MarshallingDelegates<TKey>.ToNative? keyToNative,
        MarshallingDelegates<TValue>.FromNative valueFromNative, MarshallingDelegates<TValue>.ToNative? valueToNative)
    {
        Helper = new ScriptMapHelper(mapProperty, address);
        NativeProperty = mapProperty;
        Map = (FScriptMap*)address;
        KeyFromNative = keyFromNative;
        KeyToNative = keyToNative;
        ValueFromNative = valueFromNative;
        ValueToNative = valueToNative;
        
        IntPtr keyPropertyAddress = FMapPropertyExporter.CallGetKeyProperty(NativeProperty);
        KeyProperty = new NativeProperty(keyPropertyAddress);
        
        IntPtr valuePropertyAddress = FMapPropertyExporter.CallGetValueProperty(NativeProperty);
        ValueProperty = new NativeProperty(valuePropertyAddress);
    }
    
    internal int GetMaxIndex()
    {
        return Helper.GetMaxIndex();
    }
    
    internal bool IsValidIndex(int index)
    {
        return Helper.IsValidIndex(index);
    }
    
    internal bool GetPairPtr(int index, out IntPtr keyPtr, out IntPtr valuePtr)
    {
        return Helper.GetPairPtr(index, out keyPtr, out valuePtr);
    }

    protected void ClearInternal()
    {
        Helper.EmptyValues();
    }

    protected void AddInternal(TKey key, TValue value)
    {
        Helper.AddPair(key, value, KeyToNative, ValueToNative);
    }

    protected bool RemoveInternal(TKey key)
    {
        int index = IndexOf(key);
        if (index < 0)
        {
            return false;
        }
        
        Helper.RemoveAt(index);
        return true;
    }

    protected bool TryGetInternal(TKey key, out TValue? value)
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

    protected KeyValuePair<TKey, TValue> GetAt(int index)
    {
        if (!IsValidIndex(index))
        {
            throw new IndexOutOfRangeException($"Index {index} is invalid.");
        }

        if (!GetPairPtr(index, out var keyPtr, out var valuePtr))
        {
            return default;
        }
        
        return new KeyValuePair<TKey, TValue>(KeyFromNative(keyPtr, 0), ValueFromNative(valuePtr, 0));
    }

    /// <summary>
    /// Get the value associated with the key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public TValue? Get(TKey key)
    {
        int index = IndexOf(key);
        return index >= 0 ? GetAt(index).Value : default;
    }

    /// <summary>
    /// Check if the map contains the key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool ContainsKey(TKey key)
    {
        return IndexOf(key) >= 0;
    }

    /// <summary>
    /// Check if the map contains the value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool ContainsValue(TValue value)
    {
        EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
        int maxIndex = GetMaxIndex();
        for (int i = 0; i < maxIndex; ++i)
        {
            if (IsValidIndex(i) && comparer.Equals(GetAt(i).Value, value))
            {
                return true;
            }
        }
        return false;
    }
        
    /// <summary>
    /// Get the index of the key in the map.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected int IndexOf(TKey key)
    {
        byte* keyBuffer = stackalloc byte[KeyProperty.Size];
        IntPtr keyPtr = new IntPtr(keyBuffer);
        KeyProperty.InitializeValue(keyPtr);
        KeyToNative(keyPtr, 0, key);
        return FScriptMapHelperExporter.CallFindMapPairIndexFromHash(NativeProperty, Address, keyPtr);
    }
    
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc />
    public struct Enumerator(MapBase<TKey, TValue> map) : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private int index = -1;

        public KeyValuePair<TKey, TValue> Current => map.GetAt(index);

        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            int maxIndex = map.GetMaxIndex();
            while (++index < maxIndex && !map.IsValidIndex(index)) { }
            return index < maxIndex;
        }

        /// <inheritdoc />
        public void Reset()
        {
            index = -1;
        }
    }

    public struct KeyEnumerator : ICollection<TKey>
    {
        private MapBase<TKey, TValue> map;

        public KeyEnumerator(MapBase<TKey, TValue> map)
        {
            this.map = map;
        }

        public int Count => map.Count;

        public bool IsReadOnly => true;

        public void Add(TKey item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TKey item)
        {
            return map.ContainsKey(item);
        }

        public void CopyTo(TKey[] array, int arrayIndex)
        {
            int maxIndex = map.GetMaxIndex();
            int index = arrayIndex;
            for (int i = 0; i < maxIndex; ++i)
            {
                if (map.IsValidIndex(i))
                {
                    array[index++] = map.GetAt(i).Key;
                }
            }
        }

        public bool Remove(TKey item)
        {
            throw new NotSupportedException();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(map);
        }

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
        {
            return new Enumerator(map);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(map);
        }

        public struct Enumerator : IEnumerator<TKey>
        {
            private int index;
            private MapBase<TKey, TValue> map;

            public int Count => map.Count;

            public Enumerator(MapBase<TKey, TValue> map)
            {
                this.map = map;
                index = -1;
            }

            public TKey Current => map.GetAt(index).Key;
            object IEnumerator.Current => Current;

            public void Dispose()
            {
                
            }

            public bool MoveNext()
            {
                int maxIndex = map.GetMaxIndex();
                while (++index < maxIndex && !map.IsValidIndex(index)) { }
                return index < maxIndex;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public struct ValueCollection : ICollection<TValue>
    {
        private MapBase<TKey, TValue> map;

        public ValueCollection(MapBase<TKey, TValue> map)
        {
            this.map = map;
        }

        public int Count => map.Count;

        public bool IsReadOnly => true;

        public void Add(TValue item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TValue item)
        {
            return map.ContainsValue(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            int maxIndex = map.GetMaxIndex();
            int index = arrayIndex;
            for (int i = 0; i < maxIndex; ++i)
            {
                if (map.IsValidIndex(i))
                {
                    array[index++] = map.GetAt(i).Value;
                }
            }
        }

        public bool Remove(TValue item)
        {
            throw new NotSupportedException();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(map);
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return new Enumerator(map);
        }            

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(map);
        }

        public struct Enumerator : IEnumerator<TValue>
        {
            private int index;
            private MapBase<TKey, TValue> map;

            public int Count => map.Count;

            public Enumerator(MapBase<TKey, TValue> map)
            {
                this.map = map;
                index = -1;
            }

            public TValue Current => map.GetAt(index).Value;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                int maxIndex = map.GetMaxIndex();
                while (++index < maxIndex && !map.IsValidIndex(index)) { }
                return index < maxIndex;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
}

// Used for members only
public class MapMarshaller<TKey, TValue>
{
    IntPtr nativeProperty;
    TMap<TKey, TValue>[] wrappers;
    MarshallingDelegates<TKey>.FromNative keyFromNative;
    MarshallingDelegates<TKey>.ToNative keyToNative;
    MarshallingDelegates<TValue>.FromNative valueFromNative;
    MarshallingDelegates<TValue>.ToNative valueToNative;

    public MapMarshaller(int length, IntPtr mapProperty,
        MarshallingDelegates<TKey>.ToNative keyToNative, MarshallingDelegates<TKey>.FromNative keyFromNative,
        MarshallingDelegates<TValue>.ToNative valueToNative, MarshallingDelegates<TValue>.FromNative valueFromNative)
    {
        wrappers = new TMap<TKey, TValue>[length];
        nativeProperty = mapProperty;
        this.keyFromNative = keyFromNative;
        this.keyToNative = keyToNative;
        this.valueFromNative = valueFromNative;
        this.valueToNative = valueToNative;
    }

    public TMap<TKey, TValue> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (wrappers[arrayIndex] == null)
        {
            wrappers[arrayIndex] = new TMap<TKey, TValue>(nativeProperty, 
                nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(FScriptMap)),
                keyFromNative, keyToNative, valueFromNative, valueToNative);
        }
        return wrappers[arrayIndex];
    }
}

// Used for members only where they are exposed as readonly
public class MapReadOnlyMarshaller<TKey, TValue>
{
    IntPtr nativeProperty;
    TMapReadOnly<TKey, TValue>[] wrappers;
    MarshallingDelegates<TKey>.FromNative keyFromNative;
    MarshallingDelegates<TValue>.FromNative valueFromNative;

    public MapReadOnlyMarshaller(int length, IntPtr mapProperty,
        MarshallingDelegates<TKey>.ToNative keyToNative, MarshallingDelegates<TKey>.FromNative keyFromNative, 
        MarshallingDelegates<TValue>.ToNative valueToNative, MarshallingDelegates<TValue>.FromNative valueFromNative)
    {
        nativeProperty = mapProperty;
        wrappers = new TMapReadOnly<TKey, TValue>[length];
        this.keyFromNative = keyFromNative;
        this.valueFromNative = valueFromNative;
    }

    public TMapReadOnly<TKey, TValue> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        return FromNative(nativeBuffer, 0, IntPtr.Zero);
    }

    public TMapReadOnly<TKey, TValue> FromNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop)
    {
        if (wrappers[arrayIndex] == null)
        {
            wrappers[arrayIndex] = new TMapReadOnly<TKey, TValue>(nativeProperty, nativeBuffer +
                (arrayIndex * Marshal.SizeOf(typeof(FScriptMap))), keyFromNative, valueFromNative);
        }
        
        return wrappers[arrayIndex];
    }

    public void ToNative(IntPtr nativeBuffer, IReadOnlyDictionary<TKey, TValue> value)
    {
        ToNative(nativeBuffer, 0, IntPtr.Zero, value);
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop, IReadOnlyDictionary<TKey, TValue> value)
    {
        throw new NotImplementedException("Read-only TMap cannot write to native memory.");
    }
}

// Used for function parameters / return results to copy to/from native memory
public class MapCopyMarshaller<TKey, TValue>
{
    ScriptMapHelper helper;
    MarshallingDelegates<TKey>.FromNative keyFromNative;
    MarshallingDelegates<TKey>.ToNative keyToNative;
    MarshallingDelegates<TValue>.FromNative valueFromNative;
    MarshallingDelegates<TValue>.ToNative valueToNative;

    public MapCopyMarshaller(IntPtr mapProperty,
        MarshallingDelegates<TKey>.ToNative keyToNative, MarshallingDelegates<TKey>.FromNative keyFromNative, 
        MarshallingDelegates<TValue>.ToNative valueToNative, MarshallingDelegates<TValue>.FromNative valueFromNative)
    {
        helper = new ScriptMapHelper(mapProperty);
        this.keyFromNative = keyFromNative;
        this.keyToNative = keyToNative;
        this.valueFromNative = valueFromNative;
        this.valueToNative = valueToNative;
    }

    public Dictionary<TKey, TValue> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        helper.Map = nativeBuffer;

        unsafe
        {
            FScriptMap* map = (FScriptMap*)nativeBuffer;
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();
            int maxIndex = map->GetMaxIndex();
            for (int i = 0; i < maxIndex; ++i)
            {
                if (!map->IsValidIndex(i))
                {
                    continue;
                }
                
                helper.GetPairPtr(i, out IntPtr keyPtr, out IntPtr valuePtr);
                result.Add(keyFromNative(keyPtr, 0), valueFromNative(valuePtr, 0));
            }
            return result;
        }
    }
    
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IDictionary<TKey, TValue> value)
    {
        ToNativeInternal(nativeBuffer, 0, value, ref helper, keyToNative, valueToNative);
    }
    
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IReadOnlyDictionary<TKey, TValue> value)
    {
        ToNativeInternal(nativeBuffer, 0, value.ToDictionary(), ref helper, keyToNative, valueToNative);
    }

    private void ToNativeInternal(IntPtr nativeBuffer, int arrayIndex, IDictionary<TKey, TValue> value,
        ref ScriptMapHelper helper, MarshallingDelegates<TKey>.ToNative keyToNative,
        MarshallingDelegates<TValue>.ToNative valueToNative)
    {
        IntPtr scriptMapAddress = nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(FScriptMap));
        helper.Map = scriptMapAddress;

        // Make sure any existing elements are properly destroyed
        helper.EmptyValues();

        if (value == null)
        {
            return;
        }

        Dictionary<TKey, TValue> dictionary = value as Dictionary<TKey, TValue>;
        if (dictionary != null)
        {
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                helper.AddPair(pair.Key, pair.Value, keyToNative, valueToNative);
            }

            return;
        }

        MapBase<TKey, TValue> mapBase = value as MapBase<TKey, TValue>;
        if (mapBase != null)
        {
            foreach (KeyValuePair<TKey, TValue> pair in mapBase)
            {
                helper.AddPair(pair.Key, pair.Value, keyToNative, valueToNative);
            }

            return;
        }

        foreach (KeyValuePair<TKey, TValue> pair in value)
        {
            helper.AddPair(pair.Key, pair.Value, keyToNative, valueToNative);
        }
    }
    
    public void DestructInstance(IntPtr nativeBuffer, int arrayIndex)
    {
        helper.EmptyValues();
    }
}