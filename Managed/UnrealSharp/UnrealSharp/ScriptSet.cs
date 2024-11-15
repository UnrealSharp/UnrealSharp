using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct FScriptSet
{
    public IntPtr SetPointer;
    
    internal FScriptSet(IntPtr setPointer)
    {
        SetPointer = setPointer;
    }
    
    internal bool IsValidIndex(int index)
    {
        return FScriptSetExporter.CallIsValidIndex(SetPointer, index).ToManagedBool();
    }

    internal int Num()
    {
        return FScriptSetExporter.CallNum(SetPointer);
    }

    internal int GetMaxIndex()
    {
        return FScriptSetExporter.CallGetMaxIndex(SetPointer);
    }

    internal IntPtr GetData(int index, IntPtr nativeProperty)
    {
        return FScriptSetExporter.CallGetData(index, SetPointer, nativeProperty);
    }

    internal void Empty(int slack, IntPtr nativeProperty)
    {
        FScriptSetExporter.CallEmpty(slack, SetPointer, nativeProperty);
    }

    internal void RemoveAt(int index, IntPtr nativeProperty)
    {
        FScriptSetExporter.CallRemoveAt(index, SetPointer, nativeProperty);
    }

    internal int AddUninitialized(IntPtr nativeProperty)
    {
        return FScriptSetExporter.CallAddUninitialized(SetPointer, nativeProperty);
    }
    
    internal void Add(IntPtr elementToAdd, IntPtr nativeProperty, HashDelegates.GetKeyHash elementHash, HashDelegates.Equality elementEquality, HashDelegates.Construct elementConstruct, HashDelegates.Destruct elementDestruct)
    {
        FScriptSetExporter.CallAdd(SetPointer, nativeProperty, elementToAdd, elementHash, elementEquality, elementConstruct, elementDestruct);
    }
    
    internal int FindOrAdd(IntPtr elementToAdd, IntPtr nativeProperty, HashDelegates.GetKeyHash elementHash, HashDelegates.Equality elementEquality, HashDelegates.Construct elementConstruct)
    {
        return FScriptSetExporter.CallFindOrAdd(SetPointer, nativeProperty, elementToAdd, elementHash, elementEquality, elementConstruct);
    }

    internal int FindIndex(IntPtr elementToFind, IntPtr nativeProperty, HashDelegates.GetKeyHash elementHash, HashDelegates.Equality elementEquality)
    {
        return FScriptSetExporter.CallFindIndex(SetPointer, nativeProperty, elementToFind, elementHash, elementEquality);
    }
}