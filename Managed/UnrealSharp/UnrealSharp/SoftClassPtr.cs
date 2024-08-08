using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
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
    public SoftObjectPath SoftObjectPath => _softObjectPtr.GetUniqueId();
    
    /// <summary>
    /// The class of the object.
    /// </summary>
    public TSubclassOf<T> Class => Get();
    
    public TSoftClassPtr(Type obj) : this(new TSubclassOf<T>(obj)) {}
    
    public TSoftClassPtr(TSubclassOf<T> obj)
    {
        TPersistentObjectPtrExporter.CallFromObject(ref _softObjectPtr.PersistentObjectPtrData, obj.NativeClass);
    }
    
    private TSubclassOf<T> Get()
    {
        IntPtr nativeClass = TPersistentObjectPtrExporter.CallGetNativePointer(ref _softObjectPtr.PersistentObjectPtrData);
        return new TSubclassOf<T>(nativeClass);
    }
}