using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.CSharpForUE;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Holds a soft reference to a class. Useful for holding a reference to a class that may be unloaded.
/// </summary>
/// <typeparam name="T"> The type of the object. </typeparam>
[Binding]
public struct TSoftClassPtr<T> where T : UObject
{
    private PersistentObjectPtr _softObjectPtr;
    
    /// <summary>
    /// The path to the object.
    /// </summary>
    public FSoftObjectPath SoftObjectPath => _softObjectPtr.GetUniqueId();
    
    /// <summary>
    /// The class of the object.
    /// </summary>
    public TSubclassOf<T> Class => Get();
    
    /// <summary>
    /// Is the class currently loaded?
    /// </summary>
    /// <returns> True if the class is loaded. </returns>
    public bool IsValid()
    {
        return SoftObjectPath.IsValid();
    }
    
    /// <summary>
    /// Does this soft class point to a valid class path?
    /// </summary>
    /// <returns> True if the path points to a valid class</returns>
    public bool IsNull()
    {
        return SoftObjectPath.IsNull();
    }
    
    /// <summary>
    /// Tries to get the object. Returns null if the object is not loaded.
    /// </summary>
    /// <returns> The object. </returns>
    public TSubclassOf<T> LoadSynchronous()
    {
        IntPtr handle = FSoftObjectPtrExporter.CallLoadSynchronous(ref _softObjectPtr.PersistentObjectPtrData);
        return GcHandleUtilities.GetObjectFromHandlePtr<TSubclassOf<T>>(handle);
    }

    /// <summary>
    /// Loads the class asynchronously.
    /// </summary>
    /// <param name="onLoaded"> The callback to call when the class is loaded. </param>
    public void LoadAsync(OnSoftClassLoaded onLoaded)
    {
        TSoftClassPtr<UClass> ptr = new TSoftClassPtr<UClass>(_softObjectPtr);
        UCSAsyncLoadSoftClassPtr asyncLoader = UCSAsyncLoadSoftClassPtr.AsyncLoadSoftClassPtr(ptr);
        asyncLoader.OnSuccess += onLoaded;
        asyncLoader.Activate();
    }
    
    public TSoftClassPtr(UObject obj)
    {
        _softObjectPtr = new PersistentObjectPtr(obj);
    }

    public TSoftClassPtr(Type obj) : this(new TSubclassOf<T>(obj))
    {
        
    }
    
    public TSoftClassPtr(TSubclassOf<T> obj)
    {
        TPersistentObjectPtrExporter.CallFromObject(ref _softObjectPtr.PersistentObjectPtrData, obj.NativeClass);
    }
    
    private TSoftClassPtr(PersistentObjectPtr data)
    {
        _softObjectPtr = data;
    }
    
    private TSubclassOf<T> Get()
    {
        IntPtr nativeClass = TPersistentObjectPtrExporter.CallGetNativePointer(ref _softObjectPtr.PersistentObjectPtrData);
        return new TSubclassOf<T>(nativeClass);
    }
}