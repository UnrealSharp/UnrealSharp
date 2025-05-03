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
    
    public bool IsValid()
    {
        return !Name.IsNone;
    }
    
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
    
    /// <summary>
    /// Loads all primary assets of this type.
    /// </summary>
    public async Task<IList<T>> LoadAssetListAsync<T>(IList<FName>? assetBundles = null) where T : UObject
    {
       return await UAssetManager.Get().LoadPrimaryAssets<T>(PrimaryAssetList, assetBundles);
    }
    
    /// <summary>
    /// Loads all primary assets of this type.
    /// </summary>
    public async Task<IList<UObject>> LoadAssetListAsync(IList<FName>? assetBundles = null)
    {
        return await UAssetManager.Get().LoadPrimaryAssets<UObject>(PrimaryAssetList, assetBundles);
    }
    
    /// <summary>
    /// Loads all primary asset classes of this type.
    /// </summary>
    public async Task<IList<TSubclassOf<T>>> LoadClassListAsync<T>(IList<FName>? assetBundles = null) where T : UObject
    {
        return await UAssetManager.Get().LoadPrimaryAssetClasses<T>(PrimaryAssetList, assetBundles);
    }
    
    /// <summary>
    /// Loads all primary asset classes of this type.
    /// </summary>
    public async Task<IList<TSubclassOf<UObject>>> LoadClassListAsync(IList<FName>? assetBundles = null)
    {
        return await UAssetManager.Get().LoadPrimaryAssetClasses<UObject>(PrimaryAssetList, assetBundles);
    }
}