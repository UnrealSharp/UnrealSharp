using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

public class UnrealArrayEnumerator<T>(UnrealArrayBase<T> array) : IEnumerator<T>
{
    private int _index = -1;
    public T Current => array.Get(_index);
    
    object System.Collections.IEnumerator.Current => Current;

    public void Dispose()
    {
        
    }
    
    public bool MoveNext()
    {
        ++_index;
        return _index < array.Count;
    }

    public void Reset()
    {
        _index = -1;
    }
}

public abstract unsafe class UnrealArrayBase<T> : IEnumerable<T>
{
    protected readonly IntPtr NativeProperty;
    protected MarshallingDelegates<T>.FromNative FromNative;
    protected MarshallingDelegates<T>.ToNative ToNative;
    
    protected UnmanagedArray* NativeBuffer { get; }

    [CLSCompliant(false)]
    protected UnrealArrayBase(IntPtr nativeProperty, IntPtr nativeBuffer, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
    {
        NativeProperty = nativeProperty;
        NativeBuffer = (UnmanagedArray*) nativeBuffer;
        FromNative = fromNative;
        ToNative = toNative;
    }

    /// <summary>
    /// The number of elements in the array.
    /// </summary>
    public int Count => NativeBuffer->ArrayNum;

    /// <summary>
    /// The native buffer that holds the array data.
    /// </summary>
    protected IntPtr NativeArrayBuffer => NativeBuffer->Data;

    /// <summary>
    /// Clears the array.
    /// </summary>
    protected void ClearInternal()
    {
        FArrayPropertyExporter.CallEmptyArray(NativeProperty, NativeBuffer);
    }

    /// <summary>
    /// Adds an element to the array.
    /// </summary>
    protected void AddInternal()
    {
        FArrayPropertyExporter.CallAddToArray(NativeProperty, NativeBuffer);
    }

    /// <summary>
    /// Inserts an element into the array at the specified index.
    /// </summary>
    /// <param name="index"> The index to insert the element at. </param>
    protected void InsertInternal(int index)
    {
        FArrayPropertyExporter.CallInsertInArray(NativeProperty, NativeBuffer, index);
    }

    /// <summary>
    /// Removes an element from the array at the specified index.
    /// </summary>
    /// <param name="index"> The index to remove the element at. </param>
    protected void RemoveAtInternal(int index)
    {
        FArrayPropertyExporter.CallRemoveFromArray(NativeProperty, NativeBuffer, index);
    }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index"> The index of the element to get. </param>
    /// <returns> The element at the specified index. </returns>
    /// <exception cref="IndexOutOfRangeException"> Thrown if the index is out of bounds. </exception>
    public T Get(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new IndexOutOfRangeException($"Index {index} out of bounds. Array is size {Count}");
        }
        
        return FromNative(NativeArrayBuffer, index);
    }

    /// <summary>
    /// Does the array contain the specified element?
    /// </summary>
    /// <param name="item"> The element to check for. </param>
    /// <returns> True if the element is in the array, false otherwise. </returns>
    public bool Contains(T item)
    {
        foreach (T element in this)
        {
            if (element.Equals(item))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the enumerator for the array.
    /// </summary>
    /// <returns> The enumerator for the array. </returns>
    public IEnumerator<T> GetEnumerator()
    {
        return new UnrealArrayEnumerator<T>(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class ArrayCopyMarshaller<T>
{
    private readonly IntPtr _nativeProperty;
    private readonly MarshallingDelegates<T>.ToNative _innerTypeToNative;
    private readonly MarshallingDelegates<T>.FromNative _innerTypeFromNative;

    public ArrayCopyMarshaller(IntPtr nativeProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
    {
        _nativeProperty = nativeProperty;
        _innerTypeFromNative = fromNative;
        _innerTypeToNative = toNative;
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IEnumerable<T> obj)
    {
        unsafe
        {
            UnmanagedArray* mirror = (UnmanagedArray*)(nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(UnmanagedArray)));
            if (obj == null)
            {
                FArrayPropertyExporter.CallEmptyArray(_nativeProperty, mirror);
                return;
            }

            var enumerable = obj.ToList();
            int count = enumerable.Count;
            
            FArrayPropertyExporter.CallInitializeArray(_nativeProperty, mirror, count);
            
            for (int i = 0; i < count; ++i)
            {
                _innerTypeToNative(mirror->Data, i, enumerable.ElementAt(i));
            }
        }
    }

    public List<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            List<T> result = [];
            UnmanagedArray* array = (UnmanagedArray*)nativeBuffer;
            for (int i = 0; i < array->ArrayNum; ++i)
            {
                result.Add(_innerTypeFromNative(array->Data, i));
            }
            return result;
        }
    }

    public void DestructInstance(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            UnmanagedArray* mirror = (UnmanagedArray*)(nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(UnmanagedArray)));
            FArrayPropertyExporter.CallEmptyArray(_nativeProperty, mirror);
        }
    }
}
