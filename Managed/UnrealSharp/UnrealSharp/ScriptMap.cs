using System.Runtime.InteropServices;

namespace UnrealSharp;

/// <summary>
/// Untyped map type for accessing TMap data, like FScriptArray for TArray.
/// Must have the same memory representation as a TMap.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FScriptMap
{
    public FScriptSet Pairs;
    
    public IntPtr SetPointer => Pairs.SetPointer;
    public int Count => Pairs.Num();
    
    public FScriptMap(IntPtr nativePointer)
    {
        Pairs = new FScriptSet(nativePointer);
    }

    public bool IsValidIndex(int index)
    {
        return Pairs.IsValidIndex(index);
    }

    public int Num()
    {
        return Pairs.Num();
    }

    public int GetMaxIndex()
    {
        return Pairs.GetMaxIndex();
    }

    public IntPtr GetData(int index, IntPtr nativeProperty)
    {
        return Pairs.GetData(index, nativeProperty);
    }

    public void Empty(int slack, IntPtr nativeProperty)
    {
        Pairs.Empty(slack, nativeProperty);
    }

    public void RemoveAt(int index, IntPtr nativeProperty)
    {
        Pairs.RemoveAt(index, nativeProperty);
    }

    /// <summary>
    /// Adds an uninitialized object to the map.
    /// The map will need rehashing at some point after this call to make it valid.
    /// </summary>
    /// <returns>The index of the added element.</returns>
    public int AddUninitialized(IntPtr property)
    {
        return Pairs.AddUninitialized(property);
    }
}