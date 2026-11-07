namespace UnrealSharp.CoreUObject;

public partial record struct FSoftClassPath
{
    public FSoftClassPath(FTopLevelAssetPath assetPath) : this()
    {
        AssetPath = assetPath;
    }

    public override string ToString()
    {
        string path = AssetPath.Null ? string.Empty
            : AssetPath.AssetName.IsNone ? AssetPath.PackageName.ToString()
            : $"{AssetPath.PackageName}.{AssetPath.AssetName}";
        return SubPathString.IsEmpty ? path : $"{path}:{SubPathString}";
    }
}
