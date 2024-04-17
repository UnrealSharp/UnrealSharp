using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct UnmanagedArray
{
    public IntPtr Data;
    public int ArrayNum;
    public int ArrayMax;
}

class UnrealArrayEnumerator<T>(UnrealArrayBase<T> array) : IEnumerator<T>
{
    int Index = -1;

    public T Current => array.Get(Index);

    public void Dispose()
    {
        
    }

    object System.Collections.IEnumerator.Current => Current;

    public bool MoveNext()
    {
        ++Index;
        return Index < array.Count;
    }

    public void Reset()
    {
        Index = -1;
    }
}

public abstract class UnrealArrayBase<T> : IEnumerable<T>
{
    readonly IntPtr NativeUnrealProperty;
    readonly IntPtr NativeBuffer_;
    protected MarshalingDelegates<T>.FromNative FromNative;
    protected MarshalingDelegates<T>.ToNative ToNative;

    [CLSCompliant(false)]
    public UnrealArrayBase(IntPtr nativeUnrealProperty, IntPtr nativeBuffer, MarshalingDelegates<T>.ToNative toNative, MarshalingDelegates<T>.FromNative fromNative)
    {
        NativeUnrealProperty = nativeUnrealProperty;
        NativeBuffer_ = nativeBuffer;
        FromNative = fromNative;
        ToNative = toNative;
    }

    private IntPtr NativeBuffer => NativeBuffer_;

    public int Count
    {
        get
        {
            unsafe
            {
                UnmanagedArray* nativeArray = (UnmanagedArray*) NativeBuffer.ToPointer();
                return nativeArray->ArrayNum;
            }
        }
    }

    protected IntPtr NativeArrayBuffer
    {
        get
        {
            unsafe
            {
                UnmanagedArray* nativeArray = (UnmanagedArray*)NativeBuffer.ToPointer();
                return nativeArray->Data;
            }
        }
    }

    protected void ClearInternal()
    {
        FArrayPropertyExporter.CallEmptyArray(NativeUnrealProperty, NativeBuffer);
    }

    protected void AddInternal()
    {
        FArrayPropertyExporter.CallAddToArray(NativeUnrealProperty, NativeBuffer);
    }

    protected void InsertInternal(int index)
    {
        FArrayPropertyExporter.CallInsertInArray(NativeUnrealProperty, NativeBuffer, index);
    }

    protected void RemoveAtInternal(int index)
    {
        FArrayPropertyExporter.CallRemoveFromArray(NativeUnrealProperty, NativeBuffer, index);
    }

    public T Get(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new IndexOutOfRangeException($"Index {index} out of bounds. Array is size {Count}");
        }
        
        return FromNative(NativeArrayBuffer, index);
    }

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

    public IEnumerator<T> GetEnumerator()
    {
        return new UnrealArrayEnumerator<T>(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class UnrealArrayReadOnly<T> : UnrealArrayBase<T>, IReadOnlyList<T>
{
    [CLSCompliant(false)]
    public UnrealArrayReadOnly(IntPtr nativeUnrealProperty, IntPtr nativeBuffer, MarshalingDelegates<T>.ToNative toNative, MarshalingDelegates<T>.FromNative fromNative)
        : base(nativeUnrealProperty, nativeBuffer, toNative, fromNative)
    {
    }

    public T this[int index] => Get(index);
}

public class UnrealArrayReadWrite<T> : UnrealArrayBase<T>, IList<T>
{
    [CLSCompliant(false)]
    public UnrealArrayReadWrite(IntPtr nativeUnrealProperty, IntPtr nativeBuffer, MarshalingDelegates<T>.ToNative toNative, MarshalingDelegates<T>.FromNative fromNative)
        : base(nativeUnrealProperty, nativeBuffer, toNative, fromNative)
    {
    }
    
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

    public void Add(T item)
    {
        int newIndex = Count;
        AddInternal();
        this[newIndex] = item;
    }

    public void Clear()
    {
        ClearInternal();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        int numElements = Count;
        for (int i = 0; i < numElements; ++i)
        {
            array[i + arrayIndex] = this[i];
        }
    }

    public bool IsReadOnly => false;

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index != -1)
        {
            RemoveAt(index);
        }
        return index != -1;
    }

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

    public void Insert(int index, T item)
    {
        InsertInternal(index);
        this[index] = item;
    }

    public void RemoveAt(int index)
    {
        RemoveAtInternal(index);
    }
}

public class UnrealArrayReadWriteMarshaller<T>(int length, IntPtr nativeProperty, MarshalingDelegates<T>.ToNative toNative, MarshalingDelegates<T>.FromNative fromNative)
{
    readonly UnrealArrayReadWrite<T>[] _wrappers = new UnrealArrayReadWrite<T> [length];

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, UnrealArrayReadWrite<T> obj)
    {
        throw new NotImplementedException("Copying UnrealArrays from managed memory to native memory is unsupported.");
    }

    public UnrealArrayReadWrite<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (_wrappers[arrayIndex] == null)
        {
            _wrappers[arrayIndex] = new UnrealArrayReadWrite<T>(nativeProperty, nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(UnmanagedArray)), toNative, fromNative);
        }
        return _wrappers[arrayIndex];
    }
}

public class UnrealArrayReadOnlyMarshaller<T>
{
    IntPtr NativeProperty;
    UnrealArrayReadOnly<T>[] Wrappers;
    MarshalingDelegates<T>.FromNative InnerTypeFromNative;

    public UnrealArrayReadOnlyMarshaller(int length, IntPtr nativeProperty, MarshalingDelegates<T>.ToNative toNative, MarshalingDelegates<T>.FromNative fromNative)
    {
        NativeProperty = nativeProperty;
        Wrappers = new UnrealArrayReadOnly<T>[length];
        InnerTypeFromNative = fromNative;
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, UnrealArrayReadOnly<T> obj)
    {
        throw new NotImplementedException("Copying UnrealArrays from managed memory to native memory is unsupported.");
    }

    public UnrealArrayReadOnly<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (Wrappers[arrayIndex] == null)
        {
            Wrappers[arrayIndex] = new UnrealArrayReadOnly<T>(NativeProperty, nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(UnmanagedArray)), null, InnerTypeFromNative);
        }
        return Wrappers[arrayIndex];
    }
}

public class UnrealArrayCopyMarshaller<T>
{
    int ElementSize;
    MarshalingDelegates<T>.ToNative InnerTypeToNative;
    MarshalingDelegates<T>.FromNative InnerTypeFromNative;

    public UnrealArrayCopyMarshaller(int length, MarshalingDelegates<T>.ToNative toNative, MarshalingDelegates<T>.FromNative fromNative, int elementSize)
    {
        ElementSize = elementSize;
        InnerTypeFromNative = fromNative;
        InnerTypeToNative = toNative;
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IList<T> obj)
    {
        unsafe
        {
            UnmanagedArray* mirror = (UnmanagedArray*)(nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(UnmanagedArray)));
            mirror->ArrayNum = obj.Count;
            mirror->ArrayMax = obj.Count;
            mirror->Data = Marshal.AllocCoTaskMem(obj.Count * ElementSize);

            for (int i = 0; i < obj.Count; ++i)
            {
                InnerTypeToNative(mirror->Data, i, obj[i]);
            }
        }
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IReadOnlyList<T> obj)
    {
        unsafe
        {
            UnmanagedArray* mirror = (UnmanagedArray*)(nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(UnmanagedArray)));
            mirror->ArrayNum = obj.Count;
            mirror->ArrayMax = obj.Count;
            mirror->Data = Marshal.AllocCoTaskMem(obj.Count * ElementSize);

            for (int i = 0; i < obj.Count; ++i)
            {
                InnerTypeToNative(mirror->Data, i, obj[i]);
            }
        }
    }

    public IList<T> FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        unsafe
        {
            List<T> result = [];
            UnmanagedArray* array = (UnmanagedArray*)nativeBuffer;
            for (int i = 0; i < array->ArrayNum; ++i)
            {
                result.Add(InnerTypeFromNative(array->Data, i));
            }
            return result;
        }
    }

    public static void DestructInstance (IntPtr nativeBuffer, int arrayIndex)
    {
        // Not currently used
        // unsafe
        // {
        //     UnmanagedArray* mirror = (UnmanagedArray*) (nativeBuffer + arrayIndex * sizeof(UnmanagedArray));
        //     Marshal.FreeCoTaskMem(mirror->Data);
        //     mirror->Data = IntPtr.Zero;
        //     mirror->ArrayMax = 0;
        //     mirror->ArrayNum = 0;
        // }
    }
}
