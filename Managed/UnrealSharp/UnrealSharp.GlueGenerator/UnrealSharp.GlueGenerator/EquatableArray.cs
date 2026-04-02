using System;
using System.Collections;
using System.Collections.Generic;

namespace UnrealSharp.GlueGenerator;

public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T> where T : IEquatable<T>
{
    private readonly T[]? _array;
    
    public EquatableArray(T[] array)
    {
        _array = array;
    }
    
    public bool IsNull => _array is null;
    
    public bool Equals(EquatableArray<T> array)
    {
        int aCount = Count;
        int bCount = array.Count;
        
        if (aCount != bCount)
        {
            return false;
        }

        for (int i = 0; i < aCount; i++)
        {
            if (!_array![i].Equals(array._array![i]))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        
        return obj is EquatableArray<T> array && Equals(array);
    }
    
    public override int GetHashCode()
    {
        if (_array is null)
        {
            return 0;
        }

        HashCode hashCode = default;

        foreach (T item in _array)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }
    
    public T this[int index] => _array![index];
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)(_array ?? Array.Empty<T>())).GetEnumerator();
    }    
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)(_array ?? Array.Empty<T>())).GetEnumerator();
    }

    public int Count
    {
        get
        {
            if (_array is null)
            {
                return 0;
            }

            return _array.Length;
        }
    }
    
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }
}

public readonly struct EquatableList<T> : IEquatable<EquatableList<T>>, IEnumerable<T> where T : IEquatable<T>
{
    private readonly List<T> _list;
    public List<T> List => _list;
    
    public bool IsNull => _list is null;

    public int Count
    {
        get
        {
            if (_list is null)
            {
                return 0;
            }

            return _list.Count;
        }
    }
    
    public EquatableList(List<T> list)
    {
        _list = list;
    }
    
    public bool Equals(EquatableList<T> list)
    {
        int aCount = Count;
        int bCount = list.Count;
        
        if (aCount != bCount)
        {
            return false;
        }

        for (int i = 0; i < aCount; i++)
        {
            if (!_list[i].Equals(list._list[i]))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is EquatableList<T> list && Equals(list);
    }
    
    public override int GetHashCode()
    {
        if (_list is null)
        {
            return 0;
        }

        HashCode hashCode = default;

        foreach (T item in _list)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)(_list ?? new List<T>())).GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)(_list ?? new List<T>())).GetEnumerator();
    }
}