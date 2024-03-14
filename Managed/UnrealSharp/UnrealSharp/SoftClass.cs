using UnrealSharp.Interop;

namespace UnrealSharp;

public struct SoftClass<T> where T : CoreUObject.Object
{
    private PersistentObjectPtr _softObjectPtr;
    public SoftObjectPath SoftObjectPath => _softObjectPtr.GetUniqueId();
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