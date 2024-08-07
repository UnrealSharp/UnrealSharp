using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Holds a soft reference to an object. Useful for holding a reference to an object that may be unloaded.
/// </summary>
/// <typeparam name="T"></typeparam>
[Binding]
public struct TSoftObjectPtr<T> where T : UObject
{
    internal PersistentObjectPtr SoftObjectPtr; 
    
    /// <summary>
    /// The path to the object.
    /// </summary>
    public SoftObjectPath SoftObjectPath => SoftObjectPtr.GetUniqueId();
    
    /// <summary>
    /// Tries to get the object. Returns null if the object is not loaded.
    /// </summary>
    public T? Object => Get();
    
    public TSoftObjectPtr(UObject obj)
    {
        SoftObjectPtr = new PersistentObjectPtr(obj);
    }
    
    public TSoftObjectPtr()
    {
        
    }
    
    internal TSoftObjectPtr(PersistentObjectPtrData data)
    {
        SoftObjectPtr = new PersistentObjectPtr(data);
    }
    
    /// <summary>
    /// Loads the object asynchronously.
    /// </summary>
    /// <returns></returns>
    public T LoadSynchronous()
    {
        IntPtr handle = FSoftObjectPtrExporter.CallLoadSynchronous(ref SoftObjectPtr.PersistentObjectPtrData);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    private T? Get()
    {
        var foundObject = SoftObjectPtr.Get();
        return foundObject as T;
    }
};

public static class SoftObjectMarshaller<T> where T : UObject
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, TSoftObjectPtr<T> obj)
    {
        BlittableMarshaller<PersistentObjectPtrData>.ToNative(nativeBuffer, arrayIndex, obj.SoftObjectPtr.PersistentObjectPtrData);
    }
    
    public static TSoftObjectPtr<T> FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        return new TSoftObjectPtr<T>(BlittableMarshaller<PersistentObjectPtrData>.FromNative(nativeBuffer, arrayIndex));
    }
}

