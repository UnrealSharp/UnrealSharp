using System.Collections;
using UnrealSharp.Interop.Properties;

namespace UnrealSharp;

public abstract class TSetBase<T> : IEnumerable<T>
{
    internal TSetBase(IntPtr nativeProperty, IntPtr address, MarshallingDelegates<T>.FromNative fromNative, MarshallingDelegates<T>.ToNative toNative)
    {
        FromNative = fromNative;
        ToNative = toNative;
        SetHelper = new FScriptSetHelper(new NativeProperty(nativeProperty), address);
    }

    internal FScriptSetHelper SetHelper;
    
    protected readonly MarshallingDelegates<T>.FromNative FromNative;
    protected readonly MarshallingDelegates<T>.ToNative ToNative;

    /// <summary>
    /// Amount of elements in the set.
    /// </summary>
    public int Count => SetHelper.Count;
    
    public bool Contains(T item)
    {
        if (Count == 0)
        {
            return false;
        }
        
        return IndexOf(item) >= 0;
    }
    
    public T this[int index] => Get(index);

    public T Get(int index)
    {
        if (!SetHelper.IsValidIndex(index))
        {
            throw new IndexOutOfRangeException($"Index {index} is invalid. Indices aren't necessarily sequential.");
        }
        
        return FromNative(SetHelper.GetElementPtr(index), 0);
    }

    public int IndexOf(T item)
    {
        return SetHelper.IndexOf(item, ToNative);
    }
    
    protected void ClearInternal()
    {
        SetHelper.EmptyValues();
    }

    protected void AddInternal(T item)
    {
        SetHelper.AddElement(item, ToNative);
    }
    
    protected int FindOrAddInternal(T item)
    {
        return SetHelper.FindOrAddElement(item, ToNative);
    }
    
    
    public bool IsProperSubsetOfInternal(IEnumerable<T> other)
    {
        if (other is TSetBase<T> otherAsSet)
        {
            if (Count == 0)
            {
                return otherAsSet.Count > 0;
            }
            if (Count >= otherAsSet.Count)
            {
                return false;
            }
            foreach (T item in this)
            {
                if (!otherAsSet.Contains(item))
                {
                    return false;
                }
            }
            return true;
        }

        // HashSet to avoid duplicates
        HashSet<T> set = new HashSet<T>(other);
        
        if (Count == 0)
        {
            return set.Count > 0;
        }
        
        if (Count >= set.Count)
        {
            return false;
        }
        
        foreach (T item in this)
        {
            if (!set.Contains(item))
            {
                return false;
            }
        }
        
        return true;
    }

    public bool IsProperSupersetOfInternal(IEnumerable<T> other)
    {
        if (Count == 0)
        {
            return false;
        }

        if (other is TSetBase<T> otherAsSet)
        {
            foreach (T item in otherAsSet)
            {
                if (!Contains(item))
                {
                    return false;
                }
            }
            return true;
        }

        foreach (T item in other)
        {
            if (!Contains(item))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsSubsetOfInternal(IEnumerable<T> other)
    {
        if (Count == 0)
        {
            return true;
        }

        if (other is TSetBase<T> otherAsSet)
        {
            if (Count > otherAsSet.Count)
            {
                return false;
            }

            foreach (T item in this)
            {
                if (!otherAsSet.Contains(item))
                {
                    return false;
                }
            }
            return true;
        }
        
        HashSet<T> set = new HashSet<T>(other);
        if (Count > set.Count)
        {
            return false;
        }

        foreach (T item in this)
        {
            if (!set.Contains(item))
            {
                return false;
            }
        }
        
        return true;
    }

    public bool IsSupersetOfInternal(IEnumerable<T> other)
    {
        if (other is TSetBase<T> otherAsSet)
        {
            if (otherAsSet.Count == 0)
            {
                return true;
            }
            
            if (otherAsSet.Count > Count)
            {
                return false;
            }
            
            foreach (T item in otherAsSet)
            {
                if (!Contains(item))
                {
                    return false;
                }
            }
        }
        else
        {
            foreach (T item in other)
            {
                if (!Contains(item))
                {
                    return false;
                }
            }
        }
        
        return true;
    }

    public bool OverlapsInternal(IEnumerable<T> other)
    {
        if (Count == 0)
        {
            return false;
        }

        if (other is TSetBase<T> otherAsSet)
        {
            foreach (T item in otherAsSet)
            {
                if (Contains(item))
                {
                    return true;
                }
            }
        }
        else
        {
            foreach (T item in other)
            {
                if (Contains(item))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public bool SetEqualsInternal(IEnumerable<T> other)
    {
        if (other is TSetBase<T> otherAsSet)
        {
            if (Count != otherAsSet.Count)
            {
                return false;
            }

            foreach (T item in otherAsSet)
            {
                if (!Contains(item))
                {
                    return false;
                }
            }
            return true;
        }

        foreach (T item in other)
        {
            if (!Contains(item))
            {
                return false;
            }
        }
        return true;
    }

    protected void RemoveAtInternal(int index)
    {
        if (!SetHelper.IsValidIndex(index))
        {
            return;
        }
        
        SetHelper.RemoveAt(index);
    }
    
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    public struct Enumerator(TSetBase<T> set) : IEnumerator<T>
    {
        private int _index = -1;

        public T Current => set.Get(_index);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            int maxIndex = set.SetHelper.GetMaxIndex();
            
            while (++_index < maxIndex && !set.SetHelper.IsValidIndex(_index))
            {
                
            }
            
            return _index < maxIndex;
        }

        public void Reset()
        {
            _index = -1;
        }
    }
}