using UnrealSharp.Core;
using UnrealSharp.Engine;

namespace UnrealSharp.CoreUObject;

public partial struct FPrimaryAssetId
{
    public FPrimaryAssetId(FPrimaryAssetType type, FName name)
    {
        PrimaryAssetType = type;
        PrimaryAssetName = name;
    }
    
    /// <summary>
    /// Is this a valid primary asset ID?
    /// </summary>
    /// <returns></returns>
    public bool Valid => PrimaryAssetType.Valid && !PrimaryAssetName.IsNone;
    
    public override string ToString()
    {
        return $"{PrimaryAssetType.Name.ToString()}:{PrimaryAssetName.ToString()}";
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

public static class FPrimaryAssetIdExtensions
{
    /// <summary>
    /// Asynchronously loads the asset as UObject.
    /// </summary>
    public static async Task<UObject> LoadAssetAsync(this FPrimaryAssetId assetId, List<FName>? bundles = null)
    {
        return await UAssetManager.Get().LoadPrimaryAsset(assetId, bundles);
    }

    /// <summary>
    /// Asynchronously loads the asset as type T.
    /// </summary>
    public static async Task<T> LoadAssetAsync<T>(this FPrimaryAssetId assetId, List<FName>? bundles = null) where T : UObject
    {
        return await UAssetManager.Get().LoadPrimaryAsset<T>(assetId, bundles);
    }

    /// <summary>
    /// Asynchronously loads the asset class as base UObject.
    /// </summary>
    public static async Task<TSubclassOf<UObject>> LoadAssetClassAsync(this FPrimaryAssetId assetId, List<FName>? bundles = null)
    {
        return await UAssetManager.Get().LoadPrimaryAssetClass(assetId, bundles);
    }

    /// <summary>
    /// Asynchronously loads the asset class as type T.
    /// </summary>
    public static async Task<TSubclassOf<T>> LoadAssetClassAsync<T>(this FPrimaryAssetId assetId, List<FName>? bundles = null) where T : UObject
    {
        return await UAssetManager.Get().LoadPrimaryAssetClass<T>(assetId, bundles);
    }
}