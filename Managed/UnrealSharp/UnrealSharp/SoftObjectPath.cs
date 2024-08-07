using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential), Binding]
public class SoftObjectPath
{
    private FTopLevelAssetPath AssetPath;
    private UnmanagedArray SubPathString;
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj.GetType() == GetType() && Equals((SoftObjectPath)obj);
    }
    
    public override int GetHashCode()
    {
        return AssetPath.GetHashCode();
    }
    
    public static bool operator == (SoftObjectPath a, SoftObjectPath b)
    {
        return a.AssetPath == b.AssetPath;
    }

    public static bool operator != (SoftObjectPath a, SoftObjectPath b)
    {
        return !(a == b);
    }

    public UnrealSharpObject? ResolveObject()
    {
        if (AssetPath.IsNull())
        {
            return default;
        }

        return default;
    }
}