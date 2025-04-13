using System.Collections.Generic;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator;

public static class InclusionLists
{
    private static readonly IDictionary<string, HashSet<string>> BannedProperties = new Dictionary<string, HashSet<string>>();
    
    public static void BanProperty(string structName, string propertyName)
    {
        if (!BannedProperties.TryGetValue(structName, out var propertySet))
        {
            propertySet = new HashSet<string>();
            BannedProperties[structName] = propertySet;
        }

        propertySet.Add(propertyName);
    }
    
    public static bool HasBannedProperty(UhtProperty property)
    {
        return BannedProperties.TryGetValue(property.Outer!.SourceName, out var propertySet) && propertySet.Contains(property.SourceName);
    }
}