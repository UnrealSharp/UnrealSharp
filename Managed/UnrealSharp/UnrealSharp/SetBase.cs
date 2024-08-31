using System.Collections;
using UnrealSharp.Interop.Properties;

namespace UnrealSharp;

public unsafe class TSetBase<T> : IEnumerable<T>
{
    public TSetBase(IntPtr nativeProperty, IntPtr address, 
        MarshallingDelegates<T>.FromNative fromNative, MarshallingDelegates<T>.ToNative toNative)
    {
        Property = new NativeProperty(nativeProperty);
        _set = (FScriptSet*) address;
        FromNative = fromNative;
        ToNative = toNative;
        
        SetHelper = new FScriptSetHelper(Property, address);
    }
    
    private readonly FScriptSet* _set;
    internal NativeProperty Property;
    internal MarshallingDelegates<T>.FromNative FromNative;
    internal MarshallingDelegates<T>.ToNative ToNative;
    internal FScriptSetHelper SetHelper;
    
    /// <summary>
    /// Amount of elements in the set.
    /// </summary>
    public int Count => _set->Num();
    
    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
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

    protected void RemoveAtInternal(int index)
    {
        if (!SetHelper.IsValidIndex(index))
        {
            return;
        }
        
        SetHelper.RemoveAt(index);
    }
    
    public T Get(int index)
    {
        if (!SetHelper.IsValidIndex(index))
        {
            throw new IndexOutOfRangeException($"Index {index} is invalid. Indices aren't necessarily sequential.");
        }
        
        return FromNative(SetHelper.GetElementPtr(index), 0);
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