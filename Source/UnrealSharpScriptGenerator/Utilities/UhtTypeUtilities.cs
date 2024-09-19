using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class UhtTypeUtilities
{
    public static bool HasMetadata(this UhtType type, string metadataName)
    {
        return type.MetaData.ContainsKey(metadataName);
    }
    
    public static string GetMetadata(this UhtType type, string metadataName, int nameIndex = -1)
    {
        return type.MetaData.GetValueOrDefault(metadataName, nameIndex);
    }
}