using System.Runtime.InteropServices;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct TopLevelAssetPath(Name packageName, Name assetName)
{
    public override bool Equals(object obj)
    {
        if (obj is TopLevelAssetPath other)
        {
            return _packageName == other._packageName && _assetName == other._assetName;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return _packageName.GetHashCode() ^ _assetName.GetHashCode();
    }
    public static bool operator == (TopLevelAssetPath a, TopLevelAssetPath b)
    {
        return a._packageName == b._packageName && a._assetName == b._assetName;
    }

    public static bool operator != (TopLevelAssetPath a, TopLevelAssetPath b)
    {
        return !(a == b);
    }
    public bool IsValid()
    {
        return !_packageName.IsNone();
    }
    
    public bool IsNull()
    {
        return _assetName.IsNone();
    }
    
    /** Name of the package containing the asset e.g. /Path/To/Package */
    private Name _packageName = packageName;
    /** Name of the asset within the package e.g. 'AssetName' */
    private Name _assetName = assetName;
}