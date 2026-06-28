using System.Runtime.InteropServices;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Sequential)]
public partial record struct FTopLevelAssetPath
{
    public override int GetHashCode()
    {
        return PackageName.GetHashCode() ^ AssetName.GetHashCode();
    }

    public bool Valid => PackageName.IsNone;
    public bool Null => PackageName.IsNone;
}