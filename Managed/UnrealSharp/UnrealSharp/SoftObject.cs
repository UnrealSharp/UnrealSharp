using System.Diagnostics;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;
using UnrealSharp.Interop;
using UnrealSharp.UnrealSharpAsync;

namespace UnrealSharp;

/// <summary>
/// Holds a soft reference to an object. Useful for holding a reference to an object that may be unloaded.
/// </summary>
/// <typeparam name="T"></typeparam>
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
    public bool IsNull => SoftObjectPath.Null;

    public TSoftObjectPtr(UObject obj)
    {
        SoftObjectPtr = new FPersistentObjectPtr(obj);
    }

    public TSoftObjectPtr(FPrimaryAssetId primaryAssetId)
    {
        UAssetManager assetManager = UAssetManager.Get();
        this = assetManager.GetSoftObjectReferenceFromPrimaryAssetId<T>(primaryAssetId);
    }

    internal TSoftObjectPtr(FPersistentObjectPtrData<FSoftObjectPathUnsafe> persistentObjectPtr)
    {
        SoftObjectPtr = new FPersistentObjectPtr(persistentObjectPtr);
    }

    internal TSoftObjectPtr(FPersistentObjectPtr persistentObjectPtr)
    {
        SoftObjectPtr = persistentObjectPtr;
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
    /// Casts this SoftObject to another class.
    /// </summary>
    public TSoftObjectPtr<T2> Cast<T2>() where T2 : UObject
    {
        if (typeof(T).IsAssignableFrom(typeof(T2)) || typeof(T2).IsAssignableFrom(typeof(T)))
        {
            return new TSoftObjectPtr<T2>(SoftObjectPtr);
        }

        throw new Exception($"Cannot cast {typeof(T).Name} to {typeof(T2).Name}");
    }

    private T? Get()
    {
        var foundObject = SoftObjectPtr.Get();
        return foundObject as T;
    }
};

public static class SoftObjectPtrExtensions
{
    public static async Task<T> LoadAsync<T>(this TSoftObjectPtr<T> softObjectPtr) where T : UObject
    {
        if (softObjectPtr.IsNull)
        {
            throw new Exception($"SoftObjectPath is null: {softObjectPtr.SoftObjectPath}");
        }

        if (softObjectPtr.IsValid)
        {
            return softObjectPtr.Object!;
        }

        T loadedObject = await softObjectPtr.SoftObjectPath.LoadAsync<T>();

        if (loadedObject == null)
        {
            throw new Exception($"Failed to load object at {softObjectPtr.SoftObjectPath}");
        }

        return loadedObject;
    }

    public static async Task<List<T>> LoadAsync<T>(this IList<TSoftObjectPtr<T>> softObjectPtrs) where T : UObject
    {
        List<FSoftObjectPath> softObjectPaths = new(softObjectPtrs.Count);
        foreach (TSoftObjectPtr<T> ptr in softObjectPtrs)
        {
            softObjectPaths.Add(ptr.SoftObjectPath);
        }

        IList<UObject> loadedObjects = await softObjectPaths.LoadAsync();
        List<T> result = new List<T>(loadedObjects.Count);

        foreach (UObject path in loadedObjects)
        {
            if (path is T resolved)
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