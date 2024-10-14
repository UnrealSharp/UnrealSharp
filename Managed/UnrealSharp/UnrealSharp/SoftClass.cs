using System.Diagnostics;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.UnrealSharpCore;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Holds a soft reference to a class. Useful for holding a reference to a class that may be unloaded.
/// </summary>
/// <typeparam name="T"> The type of the object. </typeparam>
[Binding]
public struct TSoftClassPtr<T> where T : UObject
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal FPersistentObjectPtr SoftObjectPtr;

    /// <summary>
    /// The path to the object.
    /// </summary>
    public FSoftObjectPath SoftObjectPath => SoftObjectPtr.GetUniqueId();

    /// <summary>
    /// The class of the object.
    /// </summary>
    public TSubclassOf<T> Class => GetClass();

    /// <summary>
    /// Checks if the class is currently loaded.
    /// </summary>
    /// <returns> True if the class is loaded. </returns>
    public bool IsValid => SoftObjectPtr.Get() != null;

    /// <summary>
    /// Checks if this soft class points to a valid class path.
    /// </summary>
    /// <returns> True if the path points to a valid class. </returns>
    public bool IsNull => SoftObjectPath.IsNull();

    public TSoftClassPtr(UObject obj)
    {
        SoftObjectPtr = new FPersistentObjectPtr(obj);
    }

    public TSoftClassPtr(Type obj) : this(new TSubclassOf<T>(obj))
    {
    }

    public TSoftClassPtr(TSubclassOf<T> obj)
    {
        TPersistentObjectPtrExporter.CallFromObject(ref SoftObjectPtr.Data, obj.NativeClass);
    }

    /// <summary>
    /// Tries to load the class synchronously. Returns null if the class is not loaded.
    /// </summary>
    /// <returns> The class. </returns>
    public TSubclassOf<T> LoadSynchronous()
    {
        IntPtr handle = FSoftObjectPtrExporter.CallLoadSynchronous(ref SoftObjectPtr.Data);
        return GcHandleUtilities.GetObjectFromHandlePtr<TSubclassOf<T>>(handle);
    }

    /// <summary>
    /// Loads the class asynchronously.
    /// </summary>
    /// <param name="onLoaded"> The callback to call when the class is loaded. </param>
    public void LoadAsync(OnSoftClassLoaded onLoaded)
    {
        var asyncLoader = UCSAsyncLoadSoftClassPtr.AsyncLoadSoftClassPtr(SoftObjectPath);
        asyncLoader.OnSuccess += onLoaded;
        asyncLoader.Activate();
    }

    public override string ToString()
    {
        return SoftObjectPath.ToString();
    }

    internal TSoftClassPtr(FPersistentObjectPtrData<FSoftObjectPathUnsafe> data)
    {
        SoftObjectPtr = new FPersistentObjectPtr(data);
    }

    private TSubclassOf<T> GetClass()
    {
        IntPtr nativeClass = TPersistentObjectPtrExporter.CallGetNativePointer(ref SoftObjectPtr.Data);
        return new TSubclassOf<T>(nativeClass);
    }
}

public static class SoftClassPtrExtensions
{
    public static void LoadAsync<T>(this IList<TSoftClassPtr<T>> softObjectPtr, OnSoftClassListLoaded onLoaded) where T : UObject
    {
        List<FSoftObjectPath> objectsToLoad = new List<FSoftObjectPath>(softObjectPtr.Count);
        
        foreach (var ptr in softObjectPtr)
        {
            objectsToLoad.Add(ptr.SoftObjectPath);
        }
        
        UCSAsyncLoadSoftClassPtrList asyncLoader = UCSAsyncLoadSoftClassPtrList.AsyncLoadSoftClassPtrList(objectsToLoad);
        asyncLoader.OnSuccess += onLoaded;
        asyncLoader.Activate();
    }
}

public static class SoftClassMarshaller<T> where T : UObject
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, TSoftClassPtr<T> obj)
    {
        BlittableMarshaller<FPersistentObjectPtrData<FSoftObjectPathUnsafe>>.ToNative(nativeBuffer, arrayIndex, obj.SoftObjectPtr.Data);
    }
    
    public static TSoftClassPtr<T> FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        FPersistentObjectPtrData<FSoftObjectPathUnsafe> softObjectPath = BlittableMarshaller<FPersistentObjectPtrData<FSoftObjectPathUnsafe>>.FromNative(nativeBuffer, arrayIndex);
        return new TSoftClassPtr<T>(softObjectPath);
    }
}