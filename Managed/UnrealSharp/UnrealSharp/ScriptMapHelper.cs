
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;
using UnrealSharp.Interop.Properties;

namespace UnrealSharp;

internal unsafe struct ScriptMapHelper
{
    public IntPtr MapAddress;
    
    private readonly NativeProperty _mapProperty;
    private readonly NativeProperty _keyProp;
    private readonly NativeProperty _valueProp;
    
    public ScriptMapHelper(NativeProperty mapProperty, NativeProperty key, NativeProperty value, IntPtr map = default)
    {
        MapAddress = map;
        
        _mapProperty = mapProperty;
        _keyProp = key;
        _valueProp = value;
    }
    
    public ScriptMapHelper(IntPtr mapProperty)
    {
        MapAddress = IntPtr.Zero;
        
        _mapProperty = new NativeProperty(mapProperty);
        _keyProp = new NativeProperty(FMapPropertyExporter.CallGetKey(mapProperty));
        _valueProp = new NativeProperty(FMapPropertyExporter.CallGetValue(mapProperty));
    }

    /// <summary>
    /// Index range check
    /// </summary>
    /// <param name="index">Index to check</param>
    /// <returns>true if accessing this element is legal.</returns>
    public bool IsValidIndex(int index)
    {
        return FScriptMapHelperExporter.CallIsValidIndex(_mapProperty.Property, MapAddress, index).ToManagedBool();
    }

    /// <summary>
    /// Returns the number of elements in the map.
    /// </summary>
    /// <returns>The number of elements in the map.</returns>
    public int Num()
    {
        return FScriptMapHelperExporter.CallNum(_mapProperty.Property, MapAddress);
    }

    /// <summary>
    /// Returns the (non-inclusive) maximum index of elements in the map.
    /// </summary>
    /// <returns>The (non-inclusive) maximum index of elements in the map.</returns>
    public int GetMaxIndex()
    {
        return FScriptMapHelperExporter.CallGetMaxIndex(_mapProperty.Property, MapAddress);
    }

    public bool GetPairPtr(int index, out IntPtr keyPtr, out IntPtr valuePtr)
    {
        IntPtr pairPtr = FScriptMapHelperExporter.CallGetPairPtr(_mapProperty.Property, MapAddress, index);
        
        if (pairPtr == IntPtr.Zero)
        {
            keyPtr = IntPtr.Zero;
            valuePtr = IntPtr.Zero;
            return false;
        }
        
        keyPtr = new IntPtr(pairPtr + _keyProp.Offset);
        valuePtr = new IntPtr(pairPtr + _valueProp.Offset);
        return true;
    }

    /// <summary>
    /// Remove all values from the map, calling destructors, etc as appropriate.
    /// </summary>
    /// <param name="slack">used to presize the array for a subsequent add, to avoid reallocation.</param>
    public void EmptyValues(int slack = 0)
    {
        FScriptMapHelperExporter.CallEmptyValues(_mapProperty.Property, MapAddress);
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

        FScriptMapHelperExporter.CallRemoveIndex(_mapProperty.Property, MapAddress, index);
    }
    
    public void AddPair<TKey, TValue>(TKey key, TValue value, MarshallingDelegates<TKey>.ToNative keyToNative,
        MarshallingDelegates<TValue>.ToNative valueToNative)
    {
        byte* keyBuffer = stackalloc byte[_keyProp.Size];
        IntPtr keyPtr = new IntPtr(keyBuffer);
        _keyProp.InitializeValue(keyPtr);
        keyToNative(keyPtr, 0, key);
        
        byte* valueBuffer = stackalloc byte[_valueProp.Size];
        IntPtr valuePtr = new IntPtr(valueBuffer);
        _valueProp.InitializeValue(valuePtr);
        valueToNative(valuePtr, 0, value);
        
        FScriptMapHelperExporter.CallAddPair(_mapProperty.Property, MapAddress, keyPtr, valuePtr);
        
        _keyProp.DestroyValue(keyPtr);
        _valueProp.DestroyValue(valuePtr);
    }
}
