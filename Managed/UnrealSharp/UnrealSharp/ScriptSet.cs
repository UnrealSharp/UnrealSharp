using System.Runtime.InteropServices;
using UnrealSharp.Core;
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
        return Bind_FScriptSet.CallIsValidIndex(SetPointer, index).ToManagedBool();
    }

    internal int Num()
    {
        return Bind_FScriptSet.CallNum(SetPointer);
    }

    internal int GetMaxIndex()
    {
        return Bind_FScriptSet.CallGetMaxIndex(SetPointer);
    }

    internal IntPtr GetData(int index, IntPtr nativeProperty)
    {
        return Bind_FScriptSet.CallGetData(index, SetPointer, nativeProperty);
    }

    internal void Empty(int slack, IntPtr nativeProperty)
    {
        Bind_FScriptSet.CallEmpty(slack, SetPointer, nativeProperty);
    }

    internal void RemoveAt(int index, IntPtr nativeProperty)
    {
        Bind_FScriptSet.CallRemoveAt(index, SetPointer, nativeProperty);
    }

    internal int AddUninitialized(IntPtr nativeProperty)
    {
        return Bind_FScriptSet.CallAddUninitialized(SetPointer, nativeProperty);
    }
    
    internal void Add(IntPtr elementToAdd, IntPtr nativeProperty, HashDelegates.GetKeyHash elementHash, HashDelegates.Equality elementEquality, HashDelegates.Construct elementConstruct, HashDelegates.Destruct elementDestruct)
    {
        Bind_FScriptSet.CallAdd(SetPointer, nativeProperty, elementToAdd, elementHash, elementEquality, elementConstruct, elementDestruct);
    }
    
    internal int FindOrAdd(IntPtr elementToAdd, IntPtr nativeProperty, HashDelegates.GetKeyHash elementHash, HashDelegates.Equality elementEquality, HashDelegates.Construct elementConstruct)
    {
        return Bind_FScriptSet.CallFindOrAdd(SetPointer, nativeProperty, elementToAdd, elementHash, elementEquality, elementConstruct);
    }

    internal int FindIndex(IntPtr elementToFind, IntPtr nativeProperty, HashDelegates.GetKeyHash elementHash, HashDelegates.Equality elementEquality)
    {
        return Bind_FScriptSet.CallFindIndex(SetPointer, nativeProperty, elementToFind, elementHash, elementEquality);
    }
}