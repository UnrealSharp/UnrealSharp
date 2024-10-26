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
}