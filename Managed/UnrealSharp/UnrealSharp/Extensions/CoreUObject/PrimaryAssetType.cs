using UnrealSharp.Core;
using UnrealSharp.Engine;

namespace UnrealSharp.CoreUObject;

public partial struct FPrimaryAssetType
{
    public FPrimaryAssetType(FName name)
    {
        Name = new FName(name);
    }

    public static implicit operator FPrimaryAssetType(FName name)
    {
        return new FPrimaryAssetType(new FName(name));
    }
    
    public static implicit operator FPrimaryAssetType(string type)
    {
        return new FPrimaryAssetType(new FName(type));
    }
    
    public bool Valid => !Name.IsNone;
    
    public override string ToString()
    {
        return Name.ToString();
    }
    
    /// <summary>
    /// Gets the list of primary assets of this type.
    /// </summary>
    public IList<FPrimaryAssetId> PrimaryAssetList
    {
        get
        {
            UAssetManager.Get().GetPrimaryAssetIdList(this, out var types);
            return types;
        }
    }
}

public static class FPrimaryAssetTypeExtensions
{
    /// <summary>
    /// Loads all primary assets of the given type as UObject.
    /// </summary>
    public static async Task<IList<UObject>> LoadAssetsAsync(this FPrimaryAssetType assetType, IList<FName>? bundles = null)
    {
        return await UAssetManager.Get().LoadPrimaryAssets<UObject>(assetType.PrimaryAssetList, bundles);
    }

    /// <summary>
    /// Loads all primary assets of the given type.
    /// </summary>
    public static async Task<IList<T>> LoadAssetsAsync<T>(this FPrimaryAssetType assetType, IList<FName>? bundles = null) where T : UObject
    {
        return await UAssetManager.Get().LoadPrimaryAssets<T>(assetType.PrimaryAssetList, bundles);
    }

    /// <summary>
    /// Loads all primary asset classes of the given type.
    /// </summary>
    public static async Task<IList<TSubclassOf<UObject>>> LoadAssetClassesAsync(this FPrimaryAssetType assetType, IList<FName>? bundles = null)
    {
        return await UAssetManager.Get().LoadPrimaryAssetClasses<UObject>(assetType.PrimaryAssetList, bundles);
    }

    /// <summary>
    /// Loads all primary asset classes of the given type.
    /// </summary>
    public static async Task<IList<TSubclassOf<T>>> LoadAssetClassesAsync<T>(this FPrimaryAssetType assetType, IList<FName>? bundles = null) where T : UObject
    {
        return await UAssetManager.Get().LoadPrimaryAssetClasses<T>(assetType.PrimaryAssetList, bundles);
    }
}