using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// An array that can be used to interact with Unreal Engine arrays.
/// </summary>
/// <typeparam name="T"> The type of elements in the array. </typeparam>
[Binding]
public class TArray<T> : UnrealArrayBase<T>, IList<T>, IReadOnlyList<T>
{
    /// <inheritdoc />
    public bool IsReadOnly => false;
    
    [CLSCompliant(false)]
    public TArray(IntPtr nativeUnrealProperty, IntPtr nativeBuffer, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
        : base(nativeUnrealProperty, nativeBuffer, toNative, fromNative)
    {
    }

    /// <inheritdoc />
    public T this[int index]
    {
        get 
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of bounds. Array size is {Count}.");
            }
            return Get(index);
        }
        set
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of bounds. Array size is {Count}.");
            }
            ToNative(NativeArrayBuffer, index, value);
        }
    }

    
    /// <summary>
    /// Adds an element to the end of the array.
    /// </summary>
    /// <param name="item"> The element to add. </param>
    public void Add(T item)
    {
        int newIndex = Count;
        AddInternal();
        this[newIndex] = item;
    }

    /// <summary>
    /// Removes all elements from the array.
    /// </summary>
    public void Clear()
    {
        ClearInternal();
    }
    
    /// <summary>
    /// Resizes the array to the specified size.
    /// If the new size is smaller than the current size, elements will be removed. If the new size is larger, elements will be added.
    /// </summary>
    /// <param name="newSize"> The new size of the array. </param>
    public void Resize(int newSize)
    {
        unsafe
        {
            FArrayPropertyExporter.CallResizeArray(NativeProperty, NativeBuffer, newSize);
        }
    }
    
    /// <summary>
    /// Swaps the elements at the specified indices.
    /// </summary>
    /// <param name="indexA"> The index of the first element to swap. </param>
    /// <param name="indexB"> The index of the second element to swap. </param>
    public void Swap(int indexA, int indexB)
    {
        unsafe
        {
            FArrayPropertyExporter.CallSwapValues(NativeProperty, NativeBuffer, indexA, indexB);
        }
    }

    /// <summary>
    /// Copy the elements of the array to an array starting at the specified index.
    /// </summary>
    /// <param name="array"> The array to copy the elements to. </param>
    /// <param name="arrayIndex"> The index in the array to start copying to. </param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        int numElements = Count;
        for (int i = 0; i < numElements; ++i)
        {
            array[i + arrayIndex] = this[i];
        }
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the array.
    /// </summary>
    /// <param name="item"> The object to remove. </param>
    /// <returns> True if the object was successfully removed; otherwise, false. This method also returns false if the object is not found in the array. </returns>
    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index != -1)
        {
            RemoveAt(index);
        }
        return index != -1;
    }

    /// <summary>
    /// Gets the index of the specified element in the array.
    /// </summary>
    /// <param name="item"> The element to find. </param>
    /// <returns> The index of the element in the array, or -1 if the element is not in the array. </returns>
    public int IndexOf(T item)
    {
        int numElements = Count;
        for (int i = 0; i < numElements; ++i)
        {
            if (this[i].Equals(item))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Inserts an element into the array at the specified index.
    /// </summary>
    /// <param name="index"> The index to insert the element at. </param>
    /// <param name="item"> The element to insert. </param>
    public void Insert(int index, T item)
    {
        InsertInternal(index);
        this[index] = item;
    }

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index"> The index of the element to remove. </param>
    public void RemoveAt(int index)
    {
        RemoveAtInternal(index);
    }
}

public class ArrayMarshaller<T>(IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
{
    private TArray<T>? _arrayWrapper;
    
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IList<T> obj)
    {
        unsafe
        {
            UnmanagedArray* mirror = (UnmanagedArray*)nativeBuffer;
            if (mirror->ArrayNum == obj.Count)
            {
                for (int i = 0; i < obj.Count; ++i)
                {
                    toNative(mirror->Data, i, obj[i]);
                }
            }
            else
            {
                FArrayPropertyExporter.CallResizeArray(nativeProperty, mirror, obj.Count);
                for (int i = 0; i < obj.Count; ++i)
                {
                    toNative(mirror->Data, i, obj[i]);
                }
            }
        }
    }

    public TArray<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (_arrayWrapper == null)
        {
            unsafe
            {
                _arrayWrapper = new TArray<T>(nativeProperty, nativeBuffer + arrayIndex * sizeof(UnmanagedArray), toNative, fromNative);
            }
        }
        return _arrayWrapper;
    }
}