using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Untyped set type for accessing TSet data, like FScriptArray for TArray.
/// Must have the same memory representation as a TSet.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ScriptSet
{
    public FScriptSparseArray Elements;
    public FHashAllocator Hash;
    public int HashSize;
    public int Count => Num();

    public bool IsValidIndex(int index)
    {
        return FScriptSetExporter.CallIsValidIndex(ref this, index).ToManagedBool();
    }

    public int Num()
    {
        return FScriptSetExporter.CallNum(ref this);
    }

    public int GetMaxIndex()
    {
        return FScriptSetExporter.CallGetMaxIndex(ref this);
    }

    public IntPtr GetData(int index, ref FScriptSetLayout layout)
    {
        return FScriptSetExporter.CallGetData(index, ref this, layout.Size);
    }

    public void Empty(int slack, ref FScriptSetLayout layout)
    {
        FScriptSetExporter.CallEmpty(slack, ref this, ref layout);
    }

    public void RemoveAt(int index, ref FScriptSetLayout layout)
    {
        FScriptSetExporter.CallRemoveAt(index, ref this, ref layout);
    }

    public int AddUninitialized(ref FScriptSetLayout layout)
    {
        return FScriptSetExporter.CallAddUninitialized(ref this, ref layout);
    }
}

/// <summary>
/// Either NULL or an identifier for an element of a set.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FSetElementId
{
    /// <summary>
    /// The index of the element in the set's element array.
    /// </summary>
    public int Index;

    public bool IsValidId => Index != -1;

    public static FSetElementId Default
    {
        get { return new FSetElementId(-1); }
    }

    public FSetElementId(int index)
    {
        Index = index;
    }

    public int AsInteger()
    {
        return Index;
    }

    public static FSetElementId FromInteger(int integer)
    {
        return new FSetElementId(integer);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FHashAllocator
{
    public FSetElementId InlineData;
    public IntPtr SecondaryData;
}

/// <summary>
/// Untyped sparse array type for accessing TSparseArray data, like FScriptArray for TArray.
/// Must have the same memory representation as a TSet.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FScriptSparseArray
{
    public UnmanagedArray Data;
    public FScriptBitArray AllocationFlags;
    public int FirstFreeIndex;
    public int NumFreeIndices;
}

/// <summary>
/// Untyped bit array type for accessing TBitArray data, like FScriptArray for TArray.
/// Must have the same memory representation as a TBitArray.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FScriptBitArray
{
    FDefaultBitArrayAllocator AllocatorInstance;
    public int NumBits;
    public int MaxBits;
}

//FDefaultBitArrayAllocator = TInlineAllocator<4>
//FDefaultBitArrayAllocator::ForElementType<uint32> = TInlineAllocator<4>::ForElementType<uint32>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct FDefaultBitArrayAllocator
{
    public fixed int InlineData[4];
    public IntPtr SecondaryData;
}

[StructLayout(LayoutKind.Sequential)]
public struct FScriptSetLayout
{
    public int HashNextIdOffset;
    public int HashIndexOffset;
    public int Size;
    public FScriptSparseArrayLayout SparseArrayLayout;
}

[StructLayout(LayoutKind.Sequential)]
public struct FScriptSparseArrayLayout
{
    public int Alignment;
    public int Size;
}

/// <summary>
/// Used to read/write a bit in the array as a bool.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FBitReference
{
    public IntPtr Data;// uint32&
    public uint Mask;
}