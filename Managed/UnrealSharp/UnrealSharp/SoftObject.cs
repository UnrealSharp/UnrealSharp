using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.CoreUObject;
using UnrealSharp.UnrealSharpCore;
using UnrealSharp.Interop;
using UnrealSharp.UnrealSharpAsync;

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
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    /// <summary>
    /// Loads the object asynchronously.
    /// </summary>
    /// <param name="onLoaded"> The callback to call when the object is loaded. </param>
    public async Task<T> LoadAsync()
    {
        if (IsNull)
        {
            throw new Exception($"SoftObjectPath is null: {SoftObjectPath}");
        }
        
        if (IsValid)
        {
            return Get()!;
        }
        
        IList<TSoftObjectPtr<T>> objectsToLoad = [this];
        List<T> loadedObjects = await objectsToLoad.LoadAsync();
        
        if (loadedObjects.Count == 0)
        {
            throw new Exception($"Failed to load {SoftObjectPath}");
        }
        
        return loadedObjects[0];
    }
    
    private T? Get()
    {
        var foundObject = SoftObjectPtr.Get();
        return foundObject as T;
    }
};

public static class SoftObjectPtrExtensions
{
    public static async Task<List<T>> LoadAsync<T>(this IList<TSoftObjectPtr<T>> softObjectPtrs) where T : UObject
    {
        List<FSoftObjectPath> softObjectPaths = new(softObjectPtrs.Count);
        foreach (TSoftObjectPtr<T> ptr in softObjectPtrs)
        {
            softObjectPaths.Add(ptr.SoftObjectPath);
        }

        IReadOnlyList<FSoftObjectPath> loadedPaths = await UCSAsyncLoadSoftPtr.LoadAsync(softObjectPaths);
        List<T> result = new List<T>(loadedPaths.Count);
        
        foreach (FSoftObjectPath path in loadedPaths)
        {
            if (path.ResolveObject() is T resolved)
            {
                result.Add(resolved);
            }
        }

        return result;
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

