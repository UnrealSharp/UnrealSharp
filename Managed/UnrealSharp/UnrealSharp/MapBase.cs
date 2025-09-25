using System.Collections;
using System.Runtime.InteropServices;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;
using UnrealSharp.Interop.Properties;

namespace UnrealSharp;

public unsafe class MapBase<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    private readonly MarshallingDelegates<TKey>.FromNative _keyFromNative;
    private readonly MarshallingDelegates<TKey>.ToNative _keyToNative;
    private readonly MarshallingDelegates<TValue>.FromNative _valueFromNative;
    private readonly MarshallingDelegates<TValue>.ToNative _valueToNative;

    private readonly NativeProperty _nativeProperty;
    private ScriptMapHelper _helper;

    private readonly NativeProperty _keyProperty;
    private readonly NativeProperty _valueProperty;
    
    public int Count => _helper.Num();

    public MapBase(IntPtr mapProperty, IntPtr address,
        MarshallingDelegates<TKey>.FromNative keyFromNative, MarshallingDelegates<TKey>.ToNative keyToNative,
        MarshallingDelegates<TValue>.FromNative valueFromNative, MarshallingDelegates<TValue>.ToNative valueToNative)
    {
        _nativeProperty = new NativeProperty(mapProperty);
        _keyProperty = new NativeProperty(FMapPropertyExporter.CallGetKey(mapProperty));
        _valueProperty = new NativeProperty(FMapPropertyExporter.CallGetValue(mapProperty));
        
        _helper = new ScriptMapHelper(_nativeProperty, _keyProperty, _valueProperty, address);
        _keyFromNative = keyFromNative;
        _keyToNative = keyToNative;
        _valueFromNative = valueFromNative;
        _valueToNative = valueToNative;
    }
    
    internal int GetMaxIndex()
    {
        return _helper.GetMaxIndex();
    }
    
    internal bool IsValidIndex(int index)
    {
        return _helper.IsValidIndex(index);
    }
    
    internal bool GetPairPtr(int index, out IntPtr keyPtr, out IntPtr valuePtr)
    {
        return _helper.GetPairPtr(index, out keyPtr, out valuePtr);
    }

    protected void ClearInternal()
    {
        _helper.EmptyValues();
    }

    protected void AddInternal(TKey key, TValue value)
    {
        _helper.AddPair(key, value, _keyToNative, _valueToNative);
    }

    protected bool RemoveInternal(TKey key)
    {
        int index = IndexOf(key);
        if (index < 0)
        {
            return false;
        }
        
        _helper.RemoveAt(index);
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
        
        return new KeyValuePair<TKey, TValue>(_keyFromNative(keyPtr, 0), _valueFromNative(valuePtr, 0));
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
    /// <param name="value"></param>
    /// <returns></returns>
    protected int IndexOf(TKey value)
    {
        byte* keyBuffer = stackalloc byte[_keyProperty.Size];
        IntPtr keyPtr = new IntPtr(keyBuffer);
        
        _keyProperty.InitializeValue(keyPtr);
        _keyToNative(keyPtr, 0, value);
        
        int index = FScriptMapHelperExporter.CallFindMapPairIndexFromHash(_nativeProperty.Property, _helper.MapAddress, keyPtr);
        
        _keyProperty.DestroyValue(keyPtr);
        return index;
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

            object? IEnumerator.Current => Current;

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
public class MapMarshaller<TKey, TValue> where TKey : notnull
{
    private readonly IntPtr _nativeProperty;
    private readonly MarshallingDelegates<TKey>.FromNative _keyFromNative;
    private readonly MarshallingDelegates<TKey>.ToNative _keyToNative;
    private readonly MarshallingDelegates<TValue>.FromNative _valueFromNative;
    private readonly MarshallingDelegates<TValue>.ToNative _valueToNative;

    public MapMarshaller(IntPtr mapProperty,
        MarshallingDelegates<TKey>.ToNative keyToNative, MarshallingDelegates<TKey>.FromNative keyFromNative,
        MarshallingDelegates<TValue>.ToNative valueToNative, MarshallingDelegates<TValue>.FromNative valueFromNative)
    {
        _nativeProperty = mapProperty;
        _keyFromNative = keyFromNative;
        _keyToNative = keyToNative;
        _valueFromNative = valueFromNative;
        _valueToNative = valueToNative;
    }
    
    public TMap<TKey, TValue> MakeWrapper(IntPtr nativeBuffer)
    {
        return new TMap<TKey, TValue>(_nativeProperty, nativeBuffer, _keyFromNative, _keyToNative, _valueFromNative, _valueToNative);
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IDictionary<TKey, TValue> value)
    {
        TMap<TKey, TValue> wrapper = MakeWrapper(nativeBuffer);

        wrapper.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in value)
        {
            wrapper.Add(pair.Key, pair.Value);
        }
    }
    
    public TMap<TKey, TValue> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        return MakeWrapper(nativeBuffer);
    }
}

// Used for members only where they are exposed as readonly
public class MapReadOnlyMarshaller<TKey, TValue> where TKey : notnull
{
    private readonly IntPtr _nativeProperty;
    private TMapReadOnly<TKey, TValue>? _readOnlyMapWrapper;
    private readonly MarshallingDelegates<TKey>.FromNative _keyFromNative;
    private readonly MarshallingDelegates<TKey>.ToNative _keyToNative;
    private readonly MarshallingDelegates<TValue>.FromNative _valueFromNative;
    private readonly MarshallingDelegates<TValue>.ToNative _valueToNative;

    public MapReadOnlyMarshaller(IntPtr mapProperty,
        MarshallingDelegates<TKey>.ToNative keyToNative, MarshallingDelegates<TKey>.FromNative keyFromNative, 
        MarshallingDelegates<TValue>.ToNative valueToNative, MarshallingDelegates<TValue>.FromNative valueFromNative)
    {
        _nativeProperty = mapProperty;
        _keyFromNative = keyFromNative;
        _keyToNative = keyToNative;
        _valueFromNative = valueFromNative;
        _valueToNative = valueToNative;
    }

    public TMapReadOnly<TKey, TValue> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        return FromNative(nativeBuffer, 0, IntPtr.Zero);
    }

    public TMapReadOnly<TKey, TValue> FromNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop)
    {
        if (_readOnlyMapWrapper == null)
        {
            _readOnlyMapWrapper = new TMapReadOnly<TKey, TValue>(_nativeProperty, nativeBuffer +
                (arrayIndex * Marshal.SizeOf(typeof(FScriptMap))), _keyFromNative, _keyToNative, _valueFromNative, _valueToNative);
        }
        
        return _readOnlyMapWrapper;
    }

    public TMap<TKey, TValue> MakeWrapper(IntPtr nativeBuffer)
    {
        return new TMap<TKey, TValue>(_nativeProperty, nativeBuffer, _keyFromNative, _keyToNative, _valueFromNative, _valueToNative);
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IReadOnlyDictionary<TKey, TValue> value)
    {
        TMap<TKey, TValue> wrapper = MakeWrapper(nativeBuffer);

        wrapper.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in value)
        {
            wrapper.Add(pair.Key, pair.Value);
        }
    }
}

// Used for function parameters / return results to copy to/from native memory
public class MapCopyMarshaller<TKey, TValue> where TKey : notnull
{
    private ScriptMapHelper _helper;
    private readonly MarshallingDelegates<TKey>.FromNative _keyFromNative;
    readonly MarshallingDelegates<TKey>.ToNative _keyToNative;
    readonly MarshallingDelegates<TValue>.FromNative _valueFromNative;
    readonly MarshallingDelegates<TValue>.ToNative _valueToNative;

    public MapCopyMarshaller(IntPtr mapProperty,
        MarshallingDelegates<TKey>.ToNative keyToNative, MarshallingDelegates<TKey>.FromNative keyFromNative, 
        MarshallingDelegates<TValue>.ToNative valueToNative, MarshallingDelegates<TValue>.FromNative valueFromNative)
    {
        _helper = new ScriptMapHelper(mapProperty);
        _keyFromNative = keyFromNative;
        _keyToNative = keyToNative;
        _valueFromNative = valueFromNative;
        _valueToNative = valueToNative;
    }

    public Dictionary<TKey, TValue> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        _helper.MapAddress = nativeBuffer;
        
        Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();
        int maxIndex = _helper.GetMaxIndex();
        
        for (int i = 0; i < maxIndex; ++i)
        {
            if (!_helper.IsValidIndex(i))
            {
                continue;
            }
                
            _helper.GetPairPtr(i, out IntPtr keyPtr, out IntPtr valuePtr);
            result.Add(_keyFromNative(keyPtr, 0), _valueFromNative(valuePtr, 0));
        }
        
        return result;
    }
    
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IDictionary<TKey, TValue> value)
    {
        ToNativeInternal(nativeBuffer, 0, value, ref _helper, _keyToNative, _valueToNative);
    }
    
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IReadOnlyDictionary<TKey, TValue> value)
    {
        ToNativeInternal(nativeBuffer, 0, value.ToDictionary(), ref _helper, _keyToNative, _valueToNative);
    }

    private void ToNativeInternal(IntPtr nativeBuffer, int arrayIndex, IDictionary<TKey, TValue> value,
        ref ScriptMapHelper helper, MarshallingDelegates<TKey>.ToNative keyToNative,
        MarshallingDelegates<TValue>.ToNative valueToNative)
    {
        IntPtr scriptMapAddress = nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(FScriptMap));
        helper.MapAddress = scriptMapAddress;

        // Make sure any existing elements are properly destroyed
        helper.EmptyValues();

        if (value == null)
        {
            return;
        }

        if (value is Dictionary<TKey, TValue> dictionary)
        {
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                helper.AddPair(pair.Key, pair.Value, keyToNative, valueToNative);
            }

            return;
        }

        if (value is MapBase<TKey, TValue> mapBase)
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
        _helper.EmptyValues();
    }
}