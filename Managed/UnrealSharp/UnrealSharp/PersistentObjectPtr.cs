using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct FPersistentObjectPtrData<ObjectID> where ObjectID : struct
{ 
    public WeakObjectData _weakPtr;
    public ObjectID _objectId;
}

[StructLayout(LayoutKind.Sequential)]
public struct FSoftObjectPathUnsafe
{
    public FTopLevelAssetPath AssetPath;
    public UnmanagedArray SubPathString;
}

[StructLayout(LayoutKind.Sequential)]
public struct FPersistentObjectPtr
{
    internal FPersistentObjectPtrData<FSoftObjectPathUnsafe> Data;
    
    public FPersistentObjectPtr(UObject obj)
    {
        if (!obj.IsValid)
        {
            return;
        }

        TPersistentObjectPtrExporter.CallFromObject(ref Data, obj.NativeObject);
    }
    
    internal FPersistentObjectPtr(FPersistentObjectPtrData<FSoftObjectPathUnsafe> nativeBuffer)
    {
        Data = nativeBuffer;
    }
    
    public FSoftObjectPath GetUniqueId()
    {
        IntPtr uniqueId = TPersistentObjectPtrExporter.CallGetUniqueID(ref Data);
        return FSoftObjectPathMarshaller.FromNative(uniqueId, 0);
    }
    
    public UObject? Get()
    {
        IntPtr handle = TPersistentObjectPtrExporter.CallGet(ref Data);
        return GcHandleUtilities.GetObjectFromHandlePtr<UObject>(handle);
    }
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        
        return obj.GetType() == GetType() && Equals((FPersistentObjectPtr)obj);
    }
    
    public static bool operator == (FPersistentObjectPtr a, FPersistentObjectPtr b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(FPersistentObjectPtr a, FPersistentObjectPtr b)
    {
        return !(a == b);
    }
}