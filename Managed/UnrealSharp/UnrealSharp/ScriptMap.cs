using System.Runtime.InteropServices;

namespace UnrealSharp;

/// <summary>
/// Untyped map type for accessing TMap data, like FScriptArray for TArray.
/// Must have the same memory representation as a TMap.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FScriptMap
{
    public ScriptSet Pairs;
    public int Count => Pairs.Count;

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

    public IntPtr GetData(int index, ref FScriptMapLayout layout)
    {
        return Pairs.GetData(index, ref layout.SetLayout);
    }

    public void Empty(int slack, ref FScriptMapLayout layout)
    {
        Pairs.Empty(slack, ref layout.SetLayout);
    }

    public void RemoveAt(int index, ref FScriptMapLayout layout)
    {
        Pairs.RemoveAt(index, ref layout.SetLayout);
    }

    /// <summary>
    /// Adds an uninitialized object to the map.
    /// The map will need rehashing at some point after this call to make it valid.
    /// </summary>
    /// <returns>The index of the added element.</returns>
    public int AddUninitialized(ref FScriptMapLayout layout)
    {
        return Pairs.AddUninitialized(ref layout.SetLayout);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FScriptMapLayout
{
    //public int KeyOffset;// is always at zero offset from the TPair - not stored here
    public int ValueOffset;
    public FScriptSetLayout SetLayout;
}