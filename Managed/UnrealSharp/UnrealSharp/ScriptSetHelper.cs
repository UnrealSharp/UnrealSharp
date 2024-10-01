using System.Runtime.InteropServices;
using UnrealSharp.Interop;
using UnrealSharp.Interop.Properties;

namespace UnrealSharp;

public class HashDelegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint GetKeyHash(IntPtr element);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NativeBool Equality(IntPtr a, IntPtr b);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Construct(IntPtr element);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Destruct(IntPtr element);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ConstructAndAssign(IntPtr element);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Assign(IntPtr element);
}

internal unsafe struct FScriptSetHelper
{        
    private readonly NativeProperty _setProperty;
    private readonly NativeProperty _elementProp;
    
    public FScriptSet Set;
    public int Count => Set.Num();

    public FScriptSetHelper(NativeProperty setProperty, IntPtr set = default)
    {           
        Set = new FScriptSet(set);
        _setProperty = setProperty;
        _elementProp = setProperty.GetInnerField(0)!;
    }

    /// <summary>
    /// Index range check
    /// </summary>
    /// <param name="index">Index to check</param>
    /// <returns>true if accessing this element is legal.</returns>
    internal bool IsValidIndex(int index)
    {
        return Set.IsValidIndex(index);
    }

    /// <summary>
    /// Returns the (non-inclusive) maximum index of elements in the set.
    /// </summary>
    /// <returns>The (non-inclusive) maximum index of elements in the set.</returns>
    internal int GetMaxIndex()
    {
        return Set.GetMaxIndex();
    }

    /// <summary>
    /// Static version of Num() used when you don't need to bother to construct a FScriptSetHelper. Returns the number of elements in the set.
    /// </summary>
    /// <param name="target">Pointer to the raw memory associated with a FScriptSet</param>
    /// <returns>The number of elements in the set.</returns>
    internal static int Num(IntPtr target)
    {
        return target == IntPtr.Zero ? 0 : ((FScriptSet*)target)->Num();
    }

    /// <summary>
    /// Returns a uint8 pointer to the element in the set.
    /// </summary>
    /// <param name="index">index of the item to return a pointer to.</param>
    /// <returns>Pointer to the element, or nullptr if the set is empty.</returns>
    internal IntPtr GetElementPtr(int index)
    {
        return Count == 0 ? IntPtr.Zero : Set.GetData(index, _setProperty.Property);
    }

    /// <summary>
    /// Add an uninitialized value to the end of the set.
    /// </summary>
    /// <returns>The index of the added element.</returns>
    internal int AddUninitializedValue()
    {
        return Set.AddUninitialized(_setProperty.Property);
    }

    /// <summary>
    /// Remove all values from the set, calling destructors, etc as appropriate.
    /// </summary>
    /// <param name="slack">used to presize the set for a subsequent add, to avoid reallocation.</param>
    internal void EmptyValues(int slack = 0)
    {
        int oldNum = Count;
        if (oldNum != 0)
        {
            DestructItems(0, oldNum);
        }
        if (oldNum != 0 || slack != 0)
        {
            Set.Empty(slack, _setProperty.Property);
        }
    }

    /// <summary>
    /// Removes an element at the specified index, destroying it.
    /// </summary>
    /// <param name="index">The index of the element to remove.</param>
    /// <param name="count">The number of elements to remove.</param>
    internal void RemoveAt(int index, int count = 1)
    {
        DestructItems(index, count);
        for (int i = 0; i < count; ++i)
        {
            if (!IsValidIndex(index))
            {
                continue;
            }
            
            Set.RemoveAt(index, _setProperty.Property);
            --count;
        }
    }

    /// <summary>
    /// Finds the index of an element in a set
    /// </summary>
    /// <param name="elementToFind">The address of an element to search for.</param>
    /// <param name="indexHint">The index to start searching from.</param>
    /// <returns>The index of an element found in SetHelper, or -1 if none was found.The index of an element found in SetHelper, or -1 if none was found.</returns>
    internal int FindElementIndex(IntPtr elementToFind, int indexHint = 0)
    {
        int setMax = GetMaxIndex();
        if (setMax == 0)
        {
            return -1;
        }
        
        int index = indexHint;
        while (true)
        {
            if (IsValidIndex(index))
            {
                IntPtr elementToCheck = GetElementPtrWithoutCheck(index);
                if (FPropertyExporter.CallIdentical(_elementProp.Property, elementToFind, elementToCheck).ToManagedBool())
                {
                    return index;
                }
            }

            ++index;
            
            if (index == setMax)
            {
                index = 0;
            }

            if (index == indexHint)
            {
                return -1;
            }
        }
    }

    /// <summary>
    /// Finds the pair in a set which matches the key in another pair.
    /// </summary>
    /// <param name="elementToFind">The address of an element to search for.</param>
    /// <param name="indexHint">The index to start searching from.</param>
    /// <returns>A pointer to the found pair, or nullptr if none was found.</returns>
    internal IntPtr FindElementPtr(IntPtr elementToFind, int indexHint = 0)
    {
        int index = FindElementIndex(elementToFind, indexHint);
        IntPtr result = index >= 0 ? GetElementPtr(index) : IntPtr.Zero;
        return result;
    }

    /// <summary>
    /// Finds element index from hash, rather than linearly searching
    /// </summary>
    internal int FindElementIndexFromHash(IntPtr elementToFind)
    {
        NativeProperty nativeProperty = _elementProp;

        uint ElementHash(IntPtr elementKey)
        {
            return nativeProperty.GetValueTypeHash(elementKey);
        }

        NativeBool ElementEquality(IntPtr a, IntPtr b)
        {
            return nativeProperty.Identical(a, b).ToNativeBool();
        }

        return Set.FindIndex(elementToFind, _setProperty.Property, ElementHash, ElementEquality);
    }

    internal int IndexOf<T>(T item, MarshallingDelegates<T>.ToNative toNative)
    {
        byte* temp = stackalloc byte[_elementProp.Size];
        IntPtr tempPtr = (IntPtr) temp;
        
        _elementProp.InitializeValue(tempPtr);
        toNative(tempPtr, 0, item);

        int index = FindElementIndexFromHash(tempPtr);
        _elementProp.DestroyValue(tempPtr);
        return index;
    }

    internal void AddElement<T>(T item, MarshallingDelegates<T>.ToNative toNative)
    {
        byte* temp = stackalloc byte[_elementProp.Size];
        IntPtr tempPtr = (IntPtr)temp;
        
        _elementProp.InitializeValue(tempPtr);
        toNative(tempPtr, 0, item);

        AddElement(tempPtr);
        _elementProp.DestroyValue(tempPtr);
    }
    
    internal int FindOrAddElement<T>(T item, MarshallingDelegates<T>.ToNative toNative)
    {
        byte* temp = stackalloc byte[_elementProp.Size];
        IntPtr tempPtr = (IntPtr)temp;
        
        _elementProp.InitializeValue(tempPtr);
        toNative(tempPtr, 0, item);

        int index = FindOrAddElement(tempPtr);
        _elementProp.DestroyValue(tempPtr);
        return index;
    }

    /// <summary>
    /// Adds the element to the set, returning true if the element was added, or false if the element was already present
    /// </summary>
    internal void AddElement(IntPtr elementToAdd)
    {
        NativeProperty property = _elementProp;

        uint ElementHash(IntPtr elementKey)
        {
            return FPropertyExporter.CallGetValueTypeHash(property.Property, elementKey);
        }

        NativeBool ElementEquality(IntPtr a, IntPtr b)
        {
            return FPropertyExporter.CallIdentical(property.Property, a, b);
        }

        void ElementConstruct(IntPtr newElement)
        {
            property.InitializeValue(newElement);
            FPropertyExporter.CallCopySingleValue(property.Property, newElement, elementToAdd);
        }

        void ElementDestruct(IntPtr element)
        {
            if (property.HasAnyPropertyFlags(NativePropertyFlags.IsPlainOldData | NativePropertyFlags.NoDestructor))
            {
                return;
            }
            
            property.DestroyValue(element);
        }

        Set.Add(elementToAdd, _setProperty.Property, ElementHash, ElementEquality, ElementConstruct, ElementDestruct);
    }
    
    internal int FindOrAddElement(IntPtr elementToAdd)
    {
        NativeProperty property = _elementProp;

        uint ElementHash(IntPtr elementKey)
        {
            return FPropertyExporter.CallGetValueTypeHash(property.Property, elementKey);
        }

        NativeBool ElementEquality(IntPtr a, IntPtr b)
        {
            return FPropertyExporter.CallIdentical(property.Property, a, b);
        }

        void ElementConstruct(IntPtr newElement)
        {
            property.InitializeValue(newElement);
            FPropertyExporter.CallCopySingleValue(property.Property, newElement, elementToAdd);
        }

        return Set.FindOrAdd(elementToAdd, _setProperty.Property, ElementHash, ElementEquality, ElementConstruct);
    }

    /// <summary>
    /// Removes the element from the set
    /// </summary>
    internal bool RemoveElement(IntPtr elementToRemove)
    {
        NativeProperty property = _elementProp;

        uint ElementHash(IntPtr elementKey)
        {
            return property.GetValueTypeHash(elementKey);
        }

        NativeBool ElementEquality(IntPtr a, IntPtr b)
        {
            return property.Identical(a, b).ToNativeBool();
        }

        int foundIndex = Set.FindIndex(elementToRemove, _setProperty.Property, ElementHash, ElementEquality);
        
        if (foundIndex == -1)
        {
            return false;
        }
        
        RemoveAt(foundIndex);
        return true;
    }

    /// <summary>
    /// Internal function to call into the property system to destruct elements.
    /// </summary>
    internal void DestructItems(int index, int count)
    {
        if (count <= 0)
        {
            return;
        }

        bool destroyElements = !_elementProp.HasAnyPropertyFlags(NativePropertyFlags.IsPlainOldData | NativePropertyFlags.NoDestructor);

        if (!destroyElements)
        {
            return;
        }

        // TODDODODO
        int stride = 0;
        IntPtr elementPtr = GetElementPtrWithoutCheck(index);

        for (int i = 0; i < count; ++i)
        {
            if (IsValidIndex(index))
            {
                _elementProp.DestroyValue_Container(elementPtr);
                --count;
            }
            
            elementPtr += stride;
        }
    }

    /// <summary>
    /// Returns a uint8 pointer to the element in the array without checking the index.
    /// </summary>
    /// <param name="index">index of the item to return a pointer to.</param>
    /// <returns>Pointer to the element, or nullptr if the array is empty.</returns>
    internal IntPtr GetElementPtrWithoutCheck(int index)
    {
        return Set.GetData(index, _setProperty.Property);
    }
}