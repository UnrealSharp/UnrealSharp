using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.UnrealSharpCore;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Holds a soft reference to an object. Useful for holding a reference to an object that may be unloaded.
/// </summary>
/// <typeparam name="T"></typeparam>
[Binding]
public struct TSoftObjectPtr<T> where T : UObject
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal FPersistentObjectPtr SoftObjectPtr; 
    
    /// <summary>
    /// The path to the object.
    /// </summary>
    public FSoftObjectPath SoftObjectPath => SoftObjectPtr.GetUniqueId();
    
    /// <summary>
    /// Tries to get the object. Returns null if the object is not loaded.
    /// </summary>
    public T? Object => Get();
    
    /// <summary>
    /// Is the object currently loaded?
    /// </summary>
    /// <returns> True if the object is loaded. </returns>
    public bool IsValid => SoftObjectPtr.Get() != null;
    
    /// <summary>
    /// Does this soft object point to a valid object path?
    /// </summary>
    /// <returns> True if the path points to a valid object</returns>
    public bool IsNull => SoftObjectPath.IsNull();
    
    public TSoftObjectPtr(UObject obj)
    {
        SoftObjectPtr = new FPersistentObjectPtr(obj);
    }
    
    internal TSoftObjectPtr(FPersistentObjectPtrData<FSoftObjectPathUnsafe> persistentObjectPtr)
    {
        SoftObjectPtr = new FPersistentObjectPtr(persistentObjectPtr);
    }

    public override string ToString()
    {
        return SoftObjectPath.ToString();
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
        UCSAsyncLoadSoftObjectPtr asyncLoadSoftObjectPtr = UCSAsyncLoadSoftObjectPtr.AsyncLoadSoftObjectPtr(SoftObjectPath);
        asyncLoadSoftObjectPtr.OnSuccess += onLoaded;
        asyncLoadSoftObjectPtr.Activate();
    }
    
    private T? Get()
    {
        var foundObject = SoftObjectPtr.Get();
        return foundObject as T;
    }
};

public static class SoftObjectPtrExtensions
{
    public static void LoadAsync<T>(this IList<TSoftObjectPtr<T>> softObjectPtr, OnSoftObjectListLoaded onLoaded) where T : UObject
    {
        List<FSoftObjectPath> objectsToLoad = new List<FSoftObjectPath>(softObjectPtr.Count);
        
        foreach (var ptr in softObjectPtr)
        {
            objectsToLoad.Add(ptr.SoftObjectPath);
        }
        
        UCSAsyncLoadSoftObjectPtrList asyncLoader = UCSAsyncLoadSoftObjectPtrList.AsyncLoadSoftObjectPtrList(objectsToLoad);
        asyncLoader.OnSuccess += onLoaded;
        asyncLoader.Activate();
    }
}

public static class SoftObjectMarshaller<T> where T : UObject
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, TSoftObjectPtr<T> obj)
    {
        BlittableMarshaller<FPersistentObjectPtrData<FSoftObjectPathUnsafe>>.ToNative(nativeBuffer, arrayIndex, obj.SoftObjectPtr.Data);
    }
    
    public static TSoftObjectPtr<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        FPersistentObjectPtrData<FSoftObjectPathUnsafe> softObjectPath = BlittableMarshaller<FPersistentObjectPtrData<FSoftObjectPathUnsafe>>.FromNative(nativeBuffer, arrayIndex);
        return new TSoftObjectPtr<T>(softObjectPath);
    }
}

