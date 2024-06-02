
using UnrealSharp.Interop;

namespace UnrealSharp;
// A copy of the native FScriptMapHelper but using without using the VM functions
// (using CopySingleValue instead of CopySingleValueToScriptVM as we aren't working with VM memory layout)
// Engine\Source\Runtime\CoreUObject\Public\UObject\UnrealType.h

/// <summary>
/// Pseudo dynamic map. Used to work with map properties in a sensible way.
/// </summary>
public unsafe struct ScriptMapHelper
{                
    private readonly IntPtr _mapProperty;
    private FScriptMap* _map;
    private FScriptMapLayout _mapLayout;

    private readonly NativeProperty keyProp;
    private readonly NativeProperty valueProp;

    public IntPtr Map
    {
        get => (IntPtr)_map;
        set => _map = (FScriptMap*)value;
    }

    public ScriptMapHelper(IntPtr mapProperty, IntPtr map = default)
    {                        
        this._mapProperty = mapProperty;
        this._map = (FScriptMap*) map;
        _mapLayout = FMapPropertyExporter.CallGetScriptLayout(mapProperty);
        keyProp = new NativeProperty(FMapPropertyExporter.CallGetKeyProperty(mapProperty));
        valueProp = new NativeProperty(FMapPropertyExporter.CallGetValueProperty(mapProperty));
    }

    /// <summary>
    /// Index range check
    /// </summary>
    /// <param name="index">Index to check</param>
    /// <returns>true if accessing this element is legal.</returns>
    public bool IsValidIndex(int index)
    {
        return _map->IsValidIndex(index);
    }

    /// <summary>
    /// Returns the number of elements in the map.
    /// </summary>
    /// <returns>The number of elements in the map.</returns>
    public int Num()
    {
        return _map->Num();
    }

    /// <summary>
    /// Returns the (non-inclusive) maximum index of elements in the map.
    /// </summary>
    /// <returns>The (non-inclusive) maximum index of elements in the map.</returns>
    public int GetMaxIndex()
    {
        return _map->GetMaxIndex();
    }

    /// <summary>
    /// Static version of Num() used when you don't need to bother to construct a FScriptMapHelper. Returns the number of elements in the map.
    /// </summary>
    /// <param name="target">Pointer to the raw memory associated with a FScriptMap</param>
    /// <returns>The number of elements in the map.</returns>
    public static int Num(IntPtr target)
    {
        return target == null ? 0 : ((FScriptMap*)target)->Num();
    }

    /// <summary>
    /// Returns a uint8 pointer to the pair in the array
    /// </summary>
    /// <param name="index">index of the item to return a pointer to.</param>
    /// <returns>Pointer to the pair, or nullptr if the array is empty.</returns>
    public IntPtr GetPairPtr(int index)
    {
        return Num() == 0 ? IntPtr.Zero : _map->GetData(index, ref _mapLayout);
    }

    public bool GetPairPtr(int index, out IntPtr keyPtr, out IntPtr valuePtr)
    {
        IntPtr pairPtr = FScriptMapHelperExporter.CallGetPairPtr(_mapProperty, Map, index);
        
        if (pairPtr == IntPtr.Zero)
        {
            keyPtr = IntPtr.Zero;
            valuePtr = IntPtr.Zero;
            return false;
        }
        
        keyPtr = new IntPtr(pairPtr + keyProp.Offset);
        valuePtr = new IntPtr(pairPtr + valueProp.Offset);
        return true;
    }

    /// <summary>
    /// Returns a uint8 pointer to the Key (first element) in the map. Currently 
    /// identical to GetPairPtr, but provides clarity of purpose and avoids exposing
    /// implementation details of TMap.
    /// </summary>
    /// <param name="index">index of the item to return a pointer to.</param>
    /// <returns>Pointer to the key, or nullptr if the map is empty.</returns>
    public IntPtr GetKeyPtr(int index)
    {
        if (Num() == 0)
        {
            return IntPtr.Zero;
        }

        return _map->GetData(index, ref _mapLayout);// + mapLayout.KeyOffset;
    }

    /// <summary>
    /// Returns a uint8 pointer to the Value (second element) in the map.
    /// </summary>
    /// <param name="index">index of the item to return a pointer to.</param>
    /// <returns>Pointer to the value, or nullptr if the map is empty.</returns>
    public IntPtr GetValuePtr(int index)
    {
        if (Num() == 0)
        {
            return IntPtr.Zero;
        }

        return _map->GetData(index, ref _mapLayout) + _mapLayout.ValueOffset;
    }

    /// <summary>
    /// Add an uninitialized value to the end of the map.
    /// </summary>
    /// <returns>The index of the added element.</returns>
    public int AddUninitializedValue()
    {
        return _map->AddUninitialized(ref _mapLayout);
    }

    /// <summary>
    /// Remove all values from the map, calling destructors, etc as appropriate.
    /// </summary>
    /// <param name="slack">used to presize the array for a subsequent add, to avoid reallocation.</param>
    public void EmptyValues(int slack = 0)
    {
        if (slack != 0)
        {
            _map->Empty(slack, ref _mapLayout);
        }
    }

    /// <summary>
    /// Removes an element at the specified index, destroying it.
    /// The map will be invalid until the next Rehash() call.
    /// </summary>
    /// <param name="index">The index of the element to remove.</param>
    /// <param name="count"></param>
    public void RemoveAt(int index)
    {
        if (!IsValidIndex(index))
        {
            return;
        }
            
        _map->RemoveAt(index, ref _mapLayout);
    }
    
    public void AddPair<TKey, TValue>(TKey key, TValue value, MarshallingDelegates<TKey>.ToNative keyToNative,
        MarshallingDelegates<TValue>.ToNative valueToNative)
    {
        byte* keyBuffer = stackalloc byte[keyProp.Size];
        IntPtr keyPtr = new IntPtr(keyBuffer);
        keyProp.InitializeValue(keyPtr);
        keyToNative(keyPtr, 0, key);
        
        byte* valueBuffer = stackalloc byte[keyProp.Size];
        IntPtr valuePtr = new IntPtr(valueBuffer);
        keyProp.InitializeValue(valuePtr);
        valueToNative(valuePtr, 0, value);
        
        FScriptMapHelperExporter.CallAddPair(_mapProperty, new IntPtr(_map), keyPtr, valuePtr);
    }
}