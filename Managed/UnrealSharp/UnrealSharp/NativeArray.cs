using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// An blittable type only array that can be used to interact with Unreal Engine arrays in a optimized manner.
/// </summary>
/// <typeparam name="T"> The type of elements in the array. </typeparam>
[Binding]
public unsafe class TNativeArray<T> : IEnumerable<T> 
    where T : INumber<T>
{
    protected readonly IntPtr NativeUnrealProperty;
    protected UnmanagedArray* NativeBuffer { get; }

    [CLSCompliant(false)]
    public TNativeArray(IntPtr nativeUnrealProperty, IntPtr nativeBuffer)
    {
        NativeUnrealProperty = nativeUnrealProperty;
        NativeBuffer = (UnmanagedArray*)nativeBuffer;
    }

    /// <summary>
    /// The number of elements in the array.
    /// </summary>
    public int Length => NativeBuffer->ArrayNum;

    /// <summary>
    /// The native buffer that holds the array data.
    /// </summary>
    protected IntPtr NativeArrayBuffer => NativeBuffer->Data;

    /// <inheritdoc />
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of bounds. Array size is {Length}.");
            }

            return *(T*)(NativeArrayBuffer + index * Unsafe.SizeOf<T>());
        }
        set
        {
            if (index < 0 || index >= Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of bounds. Array size is {Length}.");
            }

            *(T*)(NativeArrayBuffer + index * Unsafe.SizeOf<T>()) = value;
        }
    }

    /// <summary>
    /// Copy the elements of the array to an array
    /// </summary>
    /// <param name="array"> The array to copy the elements to. </param>
    public void CopyTo(T[] array)
    {
        Span<T> source = new Span<T>(NativeArrayBuffer.ToPointer(), Length);
        Span<T> destination = new Span<T>(array);

        source.CopyTo(destination);
    }


    /// <summary>
    /// Copy the elements of the span to an array
    /// </summary>
    /// <param name="array"> The array to copy the elements to. </param>
    public void CopyTo(Span<T> span)
    {
        Span<T> source = new Span<T>(NativeArrayBuffer.ToPointer(), Length);

        source.CopyTo(span);
    }

    /// <summary>
    /// Copy the elements of the array to an array
    /// </summary>
    /// <param name="array"> The array to copy the elements from. </param>
    public void CopyFrom(T[] array)
    {
        FArrayPropertyExporter.CallResizeArray(NativeUnrealProperty, NativeBuffer, array.Length);

        Span<T> source = new Span<T>(array);
        Span<T> destination = new Span<T>(NativeArrayBuffer.ToPointer(), array.Length);

        source.CopyTo(destination);
    }


    /// <summary>
    /// Copy from a span to the array
    /// </summary>
    /// <param name="array"> The array to copy the elements from. </param>
    public void CopyFrom(ReadOnlySpan<T> span)
    {
        FArrayPropertyExporter.CallResizeArray(NativeUnrealProperty, NativeBuffer, span.Length);

        Span<T> destination = new Span<T>(NativeArrayBuffer.ToPointer(), span.Length);
        span.CopyTo(destination);
    }

    /// <summary>
    /// Gets the NativeArrayBuffer as a span
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<T> AsReadOnlySpan() 
        => new ReadOnlySpan<T>(NativeArrayBuffer.ToPointer(), Length);


    /// <summary>
    /// Gets the NativeArrayBuffer as a span
    /// </summary>
    /// <returns></returns>
    public Span<T> AsSpan() 
        => new Span<T>(NativeArrayBuffer.ToPointer(), Length);

    /// <summary>
    /// Converts TNativeArray to a normal array
    /// </summary>
    public T[] ToArray()
    {
        unsafe
        {
            return new Span<T>(NativeArrayBuffer.ToPointer(), Length).ToArray();
        }
    }

    /// <summary>
    /// Gets the enumerator for the array.
    /// </summary>
    /// <returns> The enumerator for the array. </returns>
    public IEnumerator<T> GetEnumerator()
    {
        return new UnrealNativeArrayEnumerator<T>(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class UnrealNativeArrayEnumerator<T>(TNativeArray<T> array) : IEnumerator<T>
    where T : INumber<T>
{
    private int _index = -1;
    public T Current => array[_index];

    object System.Collections.IEnumerator.Current => Current;

    public void Dispose()
    {

    }

    public bool MoveNext()
    {
        ++_index;
        return _index < array.Length;
    }

    public void Reset()
    {
        _index = -1;
    }
}

public class NativeArrayMarshaller<T>(int length, IntPtr nativeProperty) 
    where T : INumber<T>
{
    private readonly TNativeArray<T>[] _wrappers = new TNativeArray<T>[length];

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, TNativeArray<T> obj)
    {
        unsafe
        {
            UnmanagedArray* mirror = (UnmanagedArray*)(nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(UnmanagedArray)));
            FArrayPropertyExporter.CallInitializeArray(nativeProperty, mirror, obj.Length);

            Span<T> destination = new Span<T>(mirror->Data.ToPointer(), obj.Length);

            obj.AsReadOnlySpan().CopyTo(destination);
        }
    }

    public TNativeArray<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (_wrappers[arrayIndex] == null)
        {
            unsafe
            {
                _wrappers[arrayIndex] = new TNativeArray<T>(nativeProperty, nativeBuffer);
            }
        }
        return _wrappers[arrayIndex];
    }
}

public class NativeArrayCopyMarshaller<T>
    where T : unmanaged
{
    private readonly IntPtr _nativeProperty;

    public NativeArrayCopyMarshaller(IntPtr nativeProperty)
    {
        _nativeProperty = nativeProperty;
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, ReadOnlySpan<T> obj)
    {
        unsafe
        {
            UnmanagedArray* mirror = (UnmanagedArray*)(nativeBuffer + arrayIndex * Marshal.SizeOf(typeof(UnmanagedArray)));
            FArrayPropertyExporter.CallInitializeArray(_nativeProperty, mirror, obj.Length);

            Span<T> destination = new Span<T>(mirror->Data.ToPointer(), obj.Length);

            obj.CopyTo(destination);
        }
    }

    public ReadOnlySpan<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            UnmanagedArray* array = (UnmanagedArray*)nativeBuffer;

            Span<T> source = new Span<T>(array->Data.ToPointer(), array->ArrayNum);
            Span<T> destination = new T[array->ArrayNum];

            source.CopyTo(destination);

            return destination;
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
