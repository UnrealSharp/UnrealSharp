namespace UnrealSharp.CoreUObject;

public partial struct FPrimaryAssetType
{
    public FPrimaryAssetType(string name)
    {
        Name = new FName(name);
    }
    
    public bool IsValid()
    {
        return !Name.IsNone;
    }
    
    public override string ToString()
    {
        return Name.ToString();
    }
}