using UnrealSharp.Attributes;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;
using UnrealSharp.Interop.Properties;

namespace UnrealSharp;

[Binding]
public class TSet<T> : TSetBase<T>, ISet<T>, IReadOnlySet<T>
{
    public TSet(IntPtr setProperty, IntPtr address,
        MarshallingDelegates<T>.FromNative fromNative, MarshallingDelegates<T>.ToNative toNative)
        : base(setProperty, address, fromNative, toNative)
    {
    }

    public bool IsReadOnly => false;

    public bool Add(T item)
    {
        if (Contains(item))
        {
            return false;
        }
        
        AddInternal(item);
        return true;
    }

    public void Clear()
    {
        ClearInternal();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        int maxIndex = SetHelper.GetMaxIndex();
        int index = arrayIndex;
        for (int i = 0; i < maxIndex; ++i)
        {
            if (SetHelper.IsValidIndex(i))
            {
                array[index++] = Get(i);
            }
        }
    }

    public void ExceptWith(IEnumerable<T> other)
    {
        if (Count == 0)
        {
            return;
        }

        if (other is TSetBase<T> otherAsSet)
        {
            foreach (T element in otherAsSet)
            {
                Remove(element);
            }
        }
        else
        {
            foreach (var element in other)
            {
                Remove(element);
            }
        }
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        if (other is TSetBase<T> otherAsSet)
        {
            if (otherAsSet.Count == 0)
            {
                Clear();
                return;
            }

            int maxIndex = SetHelper.GetMaxIndex();
            for (int i = maxIndex - 1; i >= 0; --i)
            {
                if (!SetHelper.IsValidIndex(i))
                {
                    continue;
                }
                
                T item = Get(i);
                if (!otherAsSet.Contains(item))
                {
                    RemoveAtInternal(i);
                }
            }
        }
        else
        {
            // HashSet to avoid duplicates
            HashSet<T> set = new HashSet<T>(other);
            if (set.Count == 0)
            {
                Clear();
                return;
            }

            int maxIndex = SetHelper.GetMaxIndex();
            for (int i = maxIndex - 1; i >= 0; --i)
            {
                if (!SetHelper.IsValidIndex(i))
                {
                    continue;
                }
                
                T item = Get(i);
                if (!set.Contains(item))
                {
                    RemoveAtInternal(i);
                }
            }
        }
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        return IsProperSubsetOfInternal(other);
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        return IsProperSubsetOfInternal(other);
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        return IsProperSubsetOf(other) || SetEquals(other);
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        return IsProperSupersetOf(other) || SetEquals(other);
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        return OverlapsInternal(other);
    }

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAtInternal(index);
            return true;
        }
        return false;
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        return SetEqualsInternal(other);
    }

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        if (Count == 0)
        {
            UnionWith(other);
            return;
        }

        if (other is TSetBase<T> otherAsSet)
        {
            foreach (T item in otherAsSet)
            {
                if (!Remove(item))
                {
                    Add(item);
                }
            }
        }
        else
        {
            HashSet<T> set = new HashSet<T>(other);
            foreach (T item in set)
            {
                if (!Remove(item))
                {
                    Add(item);
                }
            }
        }
    }

    public void UnionWith(IEnumerable<T> other)
    {
        if (other is TSetBase<T> otherAsSet)
        {
            foreach (T item in otherAsSet)
            {
                if (!Contains(item))
                {
                    Add(item);
                }
            }
        }
        else
        {
            foreach (T item in other)
            {
                if (!Contains(item))
                {
                    Add(item);
                }
            }
        }
    }

    void ICollection<T>.Add(T item)
    {
        AddInternal(item);
    }
}

public class SetMarshaller<T>
{
    private readonly NativeProperty _property;
    private readonly FScriptSetHelper _helper;
    private TSet<T>? _setWrapper;
    private readonly MarshallingDelegates<T>.FromNative _elementFromNative;
    private readonly MarshallingDelegates<T>.ToNative _elementToNative;

    public SetMarshaller(IntPtr setProperty,
        MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
    {
        _property = new NativeProperty(setProperty);
        _helper = new FScriptSetHelper(_property);
        _elementFromNative = fromNative;
        _elementToNative = toNative;
    }

    public TSet<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (_setWrapper == null)
        {
            _setWrapper = new TSet<T>(_property.Property, nativeBuffer, _elementFromNative, _elementToNative);
        }

        return _setWrapper;
    }

    public void ToNative(IntPtr nativeBuffer, IEnumerable<T> value)
    {
        ToNativeInternal(nativeBuffer, 0, value, _helper, _elementToNative);
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IEnumerable<T> value)
    {
        ToNativeInternal(nativeBuffer, arrayIndex, value, _helper, _elementToNative);
    }

    internal static void ToNativeInternal(IntPtr nativeBuffer, int arrayIndex, IEnumerable<T> value,
        FScriptSetHelper helper, MarshallingDelegates<T>.ToNative elementToNative)
    {
        helper.Set = new FScriptSet(nativeBuffer);
        helper.EmptyValues();

        if (value == null)
        {
            return;
        }

        if (value is HashSet<T> hashSet)
        {
            foreach (T item in hashSet)
            {
                helper.AddElement(item, elementToNative);
            }

            return;
        }

        if (value is TSetBase<T> setBase)
        {
            foreach (T item in setBase)
            {
                helper.AddElement(item, elementToNative);
            }

            return;
        }

        foreach (T item in value)
        {
            helper.AddElement(item, elementToNative);
        }
    }
}

public class SetCopyMarshaller<T>
{
    private readonly NativeProperty _property;
    private FScriptSetHelper _helper;
    readonly MarshallingDelegates<T>.FromNative _elementFromNative;
    private readonly MarshallingDelegates<T>.ToNative _elementToNative;

    public SetCopyMarshaller(IntPtr setProperty, MarshallingDelegates<T>.ToNative toNative, MarshallingDelegates<T>.FromNative fromNative)
    {
        _property = new NativeProperty(setProperty);
        _helper = new FScriptSetHelper(_property);
        _elementFromNative = fromNative;
        _elementToNative = toNative;
    }

    public HashSet<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        _helper.Set = new FScriptSet(nativeBuffer);
        HashSet<T> result = new HashSet<T>();
            
        int maxIndex = _helper.Set.GetMaxIndex();
            
        for (int i = 0; i < maxIndex; ++i)
        {
            if (!_helper.Set.IsValidIndex(i))
            {
                continue;
            }
                
            result.Add(_elementFromNative(_helper.GetElementPtr(i), 0));
        }
        return result;
    }
    
    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IEnumerable<T> value)
    {
        SetMarshaller<T>.ToNativeInternal(nativeBuffer, 0, value, _helper, _elementToNative);
    }
    
    public void DestructInstance(IntPtr nativeBuffer, int arrayIndex)
    {
        FScriptSetExporter.CallEmpty(0, _helper.Set.SetPointer, _property.Property);
    }
}