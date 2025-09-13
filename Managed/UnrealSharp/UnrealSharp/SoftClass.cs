using System.Diagnostics;
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
    public bool IsNull => SoftObjectPath.Null;

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
    
    internal TSoftClassPtr(FPersistentObjectPtrData<FSoftObjectPathUnsafe> data)
    {
        SoftObjectPtr = new FPersistentObjectPtr(data);
    }

    /// <summary>
    /// Tries to load the class synchronously. Returns null if the class is not loaded.
    /// </summary>
    /// <returns> The class. </returns>
    public TSubclassOf<T> LoadSynchronous()
    {
        IntPtr handle = FSoftObjectPtrExporter.CallLoadSynchronous(ref SoftObjectPtr.Data);
        UClass loadedClass = GCHandleUtilities.GetObjectFromHandlePtr<UClass>(handle);
        return new TSubclassOf<T>(loadedClass);
    }
    
    /// <summary>
    /// Casts this SoftClass to another class.
    /// </summary>
    public TSoftClassPtr<T2> Cast<T2>() where T2 : UObject
    {
        if (typeof(T).IsAssignableFrom(typeof(T2)) || typeof(T2).IsAssignableFrom(typeof(T)))
        {
            return Class.Valid ? new TSoftClassPtr<T2>(Class) : new TSoftClassPtr<T2>(SoftObjectPtr.Data);
        }

        throw new Exception($"Cannot cast {typeof(T).Name} to {typeof(T2).Name}");
    }
    
    public override string ToString()
    {
        return SoftObjectPath.ToString();
    }

    private TSubclassOf<T> GetClass()
    {
        IntPtr nativeClass = TPersistentObjectPtrExporter.CallGetNativePointer(ref SoftObjectPtr.Data);
        return new TSubclassOf<T>(nativeClass);
    }
    
    public static implicit operator TSoftClassPtr<T>(TSubclassOf<T> obj) => new TSoftClassPtr<T>(obj);
    public static implicit operator TSoftClassPtr<T>(Type obj) => new TSoftClassPtr<T>(obj);
    public static implicit operator TSoftClassPtr<T>(UObject obj) => new TSoftClassPtr<T>(obj);
    public static implicit operator TSubclassOf<T>(TSoftClassPtr<T> obj) => obj.Class;
    public static implicit operator FSoftObjectPath(TSoftClassPtr<T> obj) => obj.SoftObjectPath;
}

public static class SoftClassPtrExtensions
{
    public static async Task<TSubclassOf<T>> LoadAsync<T>(this TSoftClassPtr<T> softClassPtr) where T : UObject
    {
        if (softClassPtr.IsNull)
        {
            throw new Exception($"SoftClassPtr is null: {softClassPtr}");
        }
        
        if (softClassPtr.IsValid)
        {
            return softClassPtr.Class;
        }
        
        List<FSoftObjectPath> softObjectPaths = [softClassPtr.SoftObjectPath];
        IList<UObject> loadedPaths = await softObjectPaths.LoadAsync();
        
        if (loadedPaths.Count == 0)
        {
            throw new Exception($"Failed to load or cast asset at '{softClassPtr}' to '{typeof(T).Name}'");
        }

        return new TSubclassOf<T>(loadedPaths[0].NativeObject);
    }
    
    public static async Task<List<TSubclassOf<T>>> LoadAsync<T>(this IList<TSoftClassPtr<T>> softClassPtrs) where T : UObject
    {
        List<FSoftObjectPath> softObjectPaths = new(softClassPtrs.Count);
        foreach (TSoftClassPtr<T> ptr in softClassPtrs)
        {
            softObjectPaths.Add(ptr.SoftObjectPath);
        }

        IList<UObject> loadedPaths = await softObjectPaths.LoadAsync();
        List<TSubclassOf<T>> loadedClasses = new(loadedPaths.Count);
        
        foreach (UObject loadedPath in loadedPaths)
        {
            TSubclassOf<T> loadedClass = new TSubclassOf<T>(loadedPath.NativeObject);
            loadedClasses.Add(loadedClass);
        }

        return loadedClasses;
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
