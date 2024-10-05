using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.CSharpForUE;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Holds a soft reference to an object. Useful for holding a reference to an object that may be unloaded.
/// </summary>
/// <typeparam name="T"></typeparam>
[Binding]
public struct TSoftObjectPtr<T> where T : UObject
{
    internal FPersistentObjectPtr SoftObjectPtr; 
    
    /// <summary>
    /// The path to the object.
    /// </summary>
    public FSoftObjectPath SoftObjectPath => SoftObjectPtr.GetUniqueId();
    
    /// <summary>
    /// Tries to get the object. Returns null if the object is not loaded.
    /// </summary>
    public T? Object => Get();
    
    public TSoftObjectPtr(UObject obj)
    {
        SoftObjectPtr = new FPersistentObjectPtr(obj);
    }
    
    internal TSoftObjectPtr(FPersistentObjectPtr persistentObjectPtr)
    {
        SoftObjectPtr = persistentObjectPtr;
    }
    
    internal TSoftObjectPtr(IntPtr nativeBuffer)
    {
        SoftObjectPtr = new FPersistentObjectPtr(nativeBuffer);
    }
    
    /// <summary>
    /// Is the object currently loaded?
    /// </summary>
    /// <returns> True if the object is loaded. </returns>
    public bool IsValid()
    {
        return SoftObjectPtr.Get() != null;
    }
    
    /// <summary>
    /// Does this soft object point to a valid object path?
    /// </summary>
    /// <returns> True if the path points to a valid object</returns>
    public bool IsNull()
    {
        return SoftObjectPath.IsNull();
    }
    
    /// <summary>
    /// Loads the object asynchronously.
    /// </summary>
    /// <returns></returns>
    public T LoadSynchronous()
    {
        IntPtr handle = FSoftObjectPtrExporter.CallLoadSynchronous(ref SoftObjectPtr.Data);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    /// <summary>
    /// Loads the object asynchronously.
    /// </summary>
    /// <param name="onLoaded"> The callback to call when the object is loaded. </param>
    public void LoadAsync(OnSoftObjectLoaded onLoaded)
    {
        TSoftObjectPtr<UObject> async = new TSoftObjectPtr<UObject>(SoftObjectPtr);
        UCSAsyncLoadSoftObjectPtr asyncLoadSoftObjectPtr = UCSAsyncLoadSoftObjectPtr.AsyncLoadSoftObjectPtr(async);
        asyncLoadSoftObjectPtr.OnSuccess += onLoaded;
        asyncLoadSoftObjectPtr.Activate();
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
        BlittableMarshaller<FPersistentObjectPtrData<FSoftObjectPathUnsafe>>.ToNative(nativeBuffer, arrayIndex, obj.SoftObjectPtr.Data);
    }
    
    public static TSoftObjectPtr<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        return new TSoftObjectPtr<T>(nativeBuffer);
    }
}

