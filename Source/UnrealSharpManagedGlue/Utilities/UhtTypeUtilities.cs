using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.Utilities;

public static class UhtTypeUtilities
{
    public const string NullableEnable = "NullableEnable";
    
    public static T? GetTypedOuter<T>(this UhtType type) where T : UhtType
    {
        while (type.Outer != null)
        {
            if (type.Outer is T typedOuter)
            {
                return typedOuter;
            }

            type = type.Outer;
        }
        
        return null;
    }
    
    public static bool HasMetadata(this UhtType type, string metadataName)
    {
        return type.MetaData.ContainsKey(metadataName);
    }
    
    public static string GetMetadata(this UhtType type, string metadataName, int nameIndex = -1)
    {
        return type.MetaData.GetValueOrDefault(metadataName, nameIndex);
    }
}