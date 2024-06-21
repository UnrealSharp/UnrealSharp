using System.Collections;

namespace UnrealSharp;

public unsafe class SetBase<T> : IEnumerable<T>
{
    private readonly ScriptSet* Set;

    /// <summary>
    /// Amount of elements in the set.
    /// </summary>
    public int Count => Set->Num();
    
    internal IntPtr Address => (IntPtr) Set;
    internal NativeProperty Property;
    internal MarshallingDelegates<T>.FromNative FromNative;
    internal MarshallingDelegates<T>.ToNative ToNative;
    
    public SetBase(IntPtr nativeProperty, IntPtr address, 
        MarshallingDelegates<T>.FromNative fromNative, MarshallingDelegates<T>.ToNative toNative)
    {
        Property = new NativeProperty(nativeProperty, address);
        Set = (ScriptSet*) address;
        FromNative = fromNative;
        ToNative = toNative;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}