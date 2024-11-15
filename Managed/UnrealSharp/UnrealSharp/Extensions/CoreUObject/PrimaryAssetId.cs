using UnrealSharp.Engine;

namespace UnrealSharp.CoreUObject;

public partial struct FPrimaryAssetId
{
    public FPrimaryAssetId(FPrimaryAssetType type, FName name)
    {
        PrimaryAssetType = type;
        PrimaryAssetName = name;
    }
    
    public bool IsValid()
    {
        return !PrimaryAssetType.Name.IsNone && !PrimaryAssetName.IsNone;
    }
    
    public override string ToString()
    {
        return $"{PrimaryAssetType.Name.ToString()}:{PrimaryAssetName.ToString()}";
    }

    public void LoadAsync(List<FName> bundles, OnPrimaryAssetLoaded callback)
    {
        UAssetManager.Get().LoadPrimaryAsset(this, bundles, callback);
    }
    
    public void LoadAsync(OnPrimaryAssetLoaded callback)
    {
        List<FName> bundles = [];
        LoadAsync(bundles, callback);
    }
}