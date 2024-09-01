using UnrealSharp.Interop.Properties;

namespace UnrealSharp;

public class TSet<T> : TSetBase<T>, ISet<T>
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
        int maxIndex = _setHelper.GetMaxIndex();
        int index = arrayIndex;
        for (int i = 0; i < maxIndex; ++i)
        {
            if (_setHelper.IsValidIndex(i))
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

            int maxIndex = _setHelper.GetMaxIndex();
            for (int i = maxIndex - 1; i >= 0; --i)
            {
                if (!_setHelper.IsValidIndex(i))
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

            int maxIndex = _setHelper.GetMaxIndex();
            for (int i = maxIndex - 1; i >= 0; --i)
            {
                if (!_setHelper.IsValidIndex(i))
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

// Used for members only
public class SetMarshaller<T>
{
    readonly NativeProperty _property;
    FScriptSetHelper _helper;
    readonly TSet<T>[] _wrappers;
    readonly MarshallingDelegates<T>.FromNative _elementFromNative;
    readonly MarshallingDelegates<T>.ToNative _elementToNative;

    public SetMarshaller(int length, IntPtr setProperty,
        MarshallingDelegates<T>.FromNative fromNative, MarshallingDelegates<T>.ToNative toNative)
    {
        _property = new NativeProperty(setProperty);
        _helper = new FScriptSetHelper(_property);
        _wrappers = new TSet<T>[length];
        _elementFromNative = fromNative;
        _elementToNative = toNative;
    }

    public TSet<T> FromNative(IntPtr nativeBuffer)
    {
        return FromNative(nativeBuffer, 0, IntPtr.Zero);
    }

    public TSet<T> FromNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop)
    {
        if (_wrappers[arrayIndex] == null)
        {
            _wrappers[arrayIndex] = new TSet<T>(_property.Property, _property.ValueAddress(nativeBuffer), _elementFromNative, _elementToNative);
        }

        return _wrappers[arrayIndex];
    }

    public void ToNative(IntPtr nativeBuffer, IEnumerable<T> value)
    {
        ToNativeInternal(nativeBuffer, 0, value, ref _helper, _elementToNative);
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop, IEnumerable<T> value)
    {
        ToNativeInternal(nativeBuffer, arrayIndex, value, ref _helper, _elementToNative);
    }

    internal static void ToNativeInternal(IntPtr nativeBuffer, int arrayIndex, IEnumerable<T> value,
        ref FScriptSetHelper helper, MarshallingDelegates<T>.ToNative elementToNative)
    {
        unsafe
        {
            helper.Set = nativeBuffer + arrayIndex * sizeof(FScriptSet);
            helper.EmptyValues();

            if (value == null)
            {
                return;
            }

            if (value is IList<T> list)
            {
                foreach (var t in list)
                {
                    helper.AddElement(t, elementToNative);
                }

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
}

public struct SetCopyMarshaller<T>
{
    NativeProperty property;
    FScriptSetHelper helper;
    MarshallingDelegates<T>.FromNative elementFromNative;
    MarshallingDelegates<T>.ToNative elementToNative;

    public SetCopyMarshaller(int length, IntPtr setProperty,
        MarshallingDelegates<T>.FromNative fromNative, MarshallingDelegates<T>.ToNative toNative)
    {
        property = new NativeProperty(setProperty);
        helper = new FScriptSetHelper(property);
        elementFromNative = fromNative;
        elementToNative = toNative;
    }

    public HashSet<T> FromNative(IntPtr nativeBuffer)
    {
        return FromNative(nativeBuffer, 0, IntPtr.Zero);
    }

    public HashSet<T> FromNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop)
    {
        unsafe
        {
            IntPtr scriptSetAddress = nativeBuffer + arrayIndex * sizeof(FScriptSet);
            helper.Set = scriptSetAddress;
            
            FScriptSet* set = (FScriptSet*) scriptSetAddress;
            HashSet<T> result = new HashSet<T>();
            
            int maxIndex = set->GetMaxIndex();
            
            for (int i = 0; i < maxIndex; ++i)
            {
                if (!set->IsValidIndex(i))
                {
                    continue;
                }
                
                result.Add(elementFromNative(helper.GetElementPtr(i), 0));
            }
            return result;
        }
    }

    public void ToNative(IntPtr nativeBuffer, IEnumerable<T> value)
    {
        SetMarshaller<T>.ToNativeInternal(nativeBuffer, 0, value, ref helper, elementToNative);
    }

    public void ToNative(IntPtr nativeBuffer, int arrayIndex, IntPtr prop, IEnumerable<T> value)
    {
        SetMarshaller<T>.ToNativeInternal(nativeBuffer, arrayIndex, value, ref helper, elementToNative);
    }
}