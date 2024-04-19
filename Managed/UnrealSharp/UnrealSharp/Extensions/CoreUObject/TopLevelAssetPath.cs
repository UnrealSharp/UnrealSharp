using System.Runtime.InteropServices;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Sequential)]
public partial struct TopLevelAssetPath
{
    public TopLevelAssetPath(Name packageName, Name assetName)
    {
        PackageName = packageName;
        AssetName = assetName;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is TopLevelAssetPath other)
        {
            return PackageName == other.PackageName && AssetName == other.AssetName;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return PackageName.GetHashCode() ^ AssetName.GetHashCode();
    }
    
    public static bool operator == (TopLevelAssetPath a, TopLevelAssetPath b)
    {
        return a.PackageName == b.PackageName && a.AssetName == b.AssetName;
    }

    public static bool operator != (TopLevelAssetPath a, TopLevelAssetPath b)
    {
        return !(a == b);
    }
    
    public bool IsValid()
    {
        return !PackageName.IsNone();
    }
    
    public bool IsNull()
    {
        return AssetName.IsNone();
    }
}