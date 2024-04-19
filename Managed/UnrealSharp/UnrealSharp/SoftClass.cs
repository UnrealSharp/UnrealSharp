using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>
/// Holds a soft reference to a class. Useful for holding a reference to a class that may be unloaded.
/// </summary>
/// <typeparam name="T"> The type of the object. </typeparam>
public struct SoftClass<T> where T : CoreUObject.Object
{
    private PersistentObjectPtr _softObjectPtr;
    
    /// <summary>
    /// The path to the object.
    /// </summary>
    public SoftObjectPath SoftObjectPath => _softObjectPtr.GetUniqueId();
    
    /// <summary>
    /// The class of the object.
    /// </summary>
    public SubclassOf<T> Class => Get();
    
    public SoftClass(Type obj) : this(new SubclassOf<T>(obj)) {}
    
    public SoftClass(SubclassOf<T> obj)
    {
        TPersistentObjectPtrExporter.CallFromObject(ref _softObjectPtr.PersistentObjectPtrData, obj.NativeClass);
    }
    
    private SubclassOf<T> Get()
    {
        IntPtr nativeClass = TPersistentObjectPtrExporter.CallGetNativePointer(ref _softObjectPtr.PersistentObjectPtrData);
        return new SubclassOf<T>(nativeClass);
    }
}