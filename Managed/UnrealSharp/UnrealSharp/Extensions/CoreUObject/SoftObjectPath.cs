using UnrealSharp.Attributes;


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