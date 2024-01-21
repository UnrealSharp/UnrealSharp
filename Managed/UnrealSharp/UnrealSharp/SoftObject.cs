using UnrealSharp.Interop;
using Object = UnrealSharp.CoreUObject.Object;

namespace UnrealSharp;

public struct SoftObject<T> where T : Object
{
    internal PersistentObjectPtr _softObjectPtr; 
    public SoftObjectPath SoftObjectPath => _softObjectPtr.GetUniqueId();
    public T Object => Get();
    
    public SoftObject(Object obj)
    {
        _softObjectPtr = new PersistentObjectPtr(obj);
    }
    
    public SoftObject()
    {
        
    }
    
    internal SoftObject(PersistentObjectPtrData data)
    {
        _softObjectPtr = new PersistentObjectPtr(data);
    }
    
    public T LoadSynchronous()
    {
        IntPtr handle = FSoftObjectPtrExporter.CallLoadSynchronous(ref _softObjectPtr.PersistentObjectPtrData);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    private T Get()
    {
        return (T) _softObjectPtr.Get();
    }
    
};

public static class SoftObjectMarshaler<T> where T : Object
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, SoftObject<T> obj)
    {
        BlittableMarshaller<PersistentObjectPtrData>.ToNative(nativeBuffer, arrayIndex, owner, obj._softObjectPtr.PersistentObjectPtrData);
    }
    
    public static SoftObject<T> FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        return new SoftObject<T>(BlittableMarshaller<PersistentObjectPtrData>.FromNative(nativeBuffer, arrayIndex, owner));
    }
}

