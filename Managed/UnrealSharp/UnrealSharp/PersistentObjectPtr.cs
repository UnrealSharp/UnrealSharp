using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct FPersistentObjectPtrData<ObjectId> where ObjectId : struct
{ 
    public WeakObjectData _weakPtr;
    public ObjectId _objectId;
}

[StructLayout(LayoutKind.Sequential)]
public struct FSoftObjectPathUnsafe
{
    public FTopLevelAssetPath AssetPath;
    public UnmanagedArray SubPathString;
}

[StructLayout(LayoutKind.Sequential)]
public struct FPersistentObjectPtr : IEquatable<FPersistentObjectPtr>
{
    internal FPersistentObjectPtrData<FSoftObjectPathUnsafe> Data;
    
    public FPersistentObjectPtr(UObject obj)
    {
        if (!obj.IsValid())
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
        return GCHandleUtilities.GetObjectFromHandlePtr<UObject>(handle);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FPersistentObjectPtr other)
        {
            return false;
        }

        return TPersistentObjectPtrExporter.CallEquals(ref Data, ref other.Data).ToManagedBool();
    }

    public bool Equals(FPersistentObjectPtr other)
    {
        return Equals((object)other);
    }

    public override int GetHashCode()
    {
        return TPersistentObjectPtrExporter.CallGetHashCode(ref Data);
    }
}