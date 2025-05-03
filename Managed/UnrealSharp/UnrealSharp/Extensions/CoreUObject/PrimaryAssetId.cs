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

    public async Task<T> LoadAsyncAsset<T>(List<FName>? bundles = null) where T : UObject
    {
        return await UAssetManager.Get().LoadPrimaryAsset<T>(this, bundles);
    }
    
    public async Task<UObject> LoadAsyncAsset(List<FName>? bundles = null)
    {
        return await UAssetManager.Get().LoadPrimaryAsset(this, bundles);
    }
    
    public async Task<TSubclassOf<UObject>> LoadAsyncAssetClass(List<FName>? bundles = null)
    {
        return await UAssetManager.Get().LoadPrimaryAssetClass(this, bundles);
    }
    
    public async Task<TSubclassOf<T>> LoadAsyncAssetClass<T>(List<FName>? bundles = null) where T : UObject
    {
        return await UAssetManager.Get().LoadPrimaryAssetClass<T>(this, bundles);
    }
    
    /// <summary>
    /// Gets the asset associated with this primary asset ID. Use AssetClass if this asset ID belongs to a primary asset which is a Blueprint class.
    /// </summary>
    public UObject? Asset => UAssetManager.Get().GetPrimaryAssetObject(this);
    
    /// <summary>
    /// Gets the asset class associated with this primary asset ID.
    /// </summary>
    public TSubclassOf<UObject>? AssetClass => UAssetManager.Get().GetPrimaryAssetObjectClass(this);
}