using UnrealSharp.Attributes;
using UnrealSharp.Core.Attributes;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.CoreUObject;

[Binding]
public partial struct FSoftObjectPath
{
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        
        return obj.GetType() == GetType() && Equals((FSoftObjectPath)obj);
    }

    public override string ToString()
    {
        return $"{AssetPath.PackageName}.{AssetPath.AssetName}";
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
        return UCSSoftObjectPathExtensions.ResolveObject(this);
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