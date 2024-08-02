using System.Runtime.InteropServices;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Sequential)]
public partial struct FTopLevelAssetPath
{
    public FTopLevelAssetPath(FName packageName, FName assetName)
    {
        PackageName = packageName;
        AssetName = assetName;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is FTopLevelAssetPath other)
        {
            return PackageName == other.PackageName && AssetName == other.AssetName;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return PackageName.GetHashCode() ^ AssetName.GetHashCode();
    }
    
    public static bool operator == (FTopLevelAssetPath a, FTopLevelAssetPath b)
    {
        return a.PackageName == b.PackageName && a.AssetName == b.AssetName;
    }

    public static bool operator != (FTopLevelAssetPath a, FTopLevelAssetPath b)
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