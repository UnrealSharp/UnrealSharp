using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential), Binding]
public class FSoftObjectPath
{
    private FTopLevelAssetPath AssetPath;
    private UnmanagedArray SubPathString;
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj.GetType() == GetType() && Equals((FSoftObjectPath)obj);
    }
    
    public override int GetHashCode()
    {
        return AssetPath.GetHashCode();
    }
    
    public bool IsValid()
    {
        return AssetPath.IsValid();
    }
    
    public bool IsNull()
    {
        return AssetPath.IsNull();
    }
    
    public UObject? ResolveObject()
    {
        if (AssetPath.IsNull())
        {
            return default;
        }

        return default;
    }
    
    public static bool operator == (FSoftObjectPath a, FSoftObjectPath b)
    {
        return a.AssetPath == b.AssetPath;
    }

    public static bool operator != (FSoftObjectPath a, FSoftObjectPath b)
    {
        return !(a == b);
    }
}