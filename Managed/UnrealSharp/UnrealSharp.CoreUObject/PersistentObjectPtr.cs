using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject.Interop;
using Object = UnrealSharp.Core.UnrealSharpObject;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Sequential)]
public struct PersistentObjectPtrData
{ 
    public readonly bool Equals(PersistentObjectPtrData other)
    {
        return Equals(_weakPtr, other._weakPtr) && Equals(_objectId, other._objectId);
    }
    
    public WeakObjectData _weakPtr;
    public SoftObjectPath _objectId;
}

[StructLayout(LayoutKind.Sequential)]
public struct PersistentObjectPtr
{
    public bool Equals(PersistentObjectPtr other)
    {
        return PersistentObjectPtrData.Equals(other.PersistentObjectPtrData);
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj.GetType() == GetType() && Equals((PersistentObjectPtr)obj);
    }
    public override int GetHashCode()
    {
        return PersistentObjectPtrData.GetHashCode();
    }

    public PersistentObjectPtr(Object obj)
    {
        if (obj == null)
        {
            return;
        }
        
        TPersistentObjectPtrExporter.CallFromObject(ref PersistentObjectPtrData, obj.NativeObject);
    }
    
    public PersistentObjectPtr(IntPtr native)
    {
        unsafe
        {
            PersistentObjectPtrData = *(PersistentObjectPtrData*) native.ToPointer(); 
        }
    }

    public PersistentObjectPtr()
    {
        
    }
    
    internal PersistentObjectPtr(PersistentObjectPtrData data)
    {
        PersistentObjectPtrData = data;
    }
    
    public static bool operator == (PersistentObjectPtr a, PersistentObjectPtr b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(PersistentObjectPtr a, PersistentObjectPtr b)
    {
        return !(a == b);
    }
    public SoftObjectPath GetUniqueId()
    {
        return PersistentObjectPtrData._objectId;
    }
    
    public Object Get()
    {
        IntPtr handle = TPersistentObjectPtrExporter.CallGet(ref PersistentObjectPtrData);
        return GcHandleUtilities.GetObjectFromHandlePtr<Object>(handle);
    }

    internal PersistentObjectPtrData PersistentObjectPtrData;
}