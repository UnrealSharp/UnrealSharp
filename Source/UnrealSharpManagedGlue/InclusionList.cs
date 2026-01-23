using EpicGames.UHT.Types;
using System.Collections.Generic;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class InclusionLists
{
    private static readonly IDictionary<string, HashSet<string>> BannedProperties = new Dictionary<string, HashSet<string>>();
    private static readonly IDictionary<string, HashSet<string>> BannedFunctions = new Dictionary<string, HashSet<string>>();
    private static readonly HashSet<string> BannedEquality = new();
    private static readonly HashSet<string> BannedArithmetic = new();

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
    
    public static void BanFunction(string structName, string functionName)
    {
        if (!BannedFunctions.TryGetValue(structName, out var functionSet))
        {
            functionSet = new HashSet<string>();
            BannedFunctions[structName] = functionSet;
        }

        functionSet.Add(functionName);
    }
    
    public static bool HasBannedFunction(UhtFunction function)
    {
        return BannedFunctions.TryGetValue(function.Outer!.SourceName, out HashSet<string>? functionSet) && functionSet.Contains(function.SourceName);
    }

    public static void BanEquality(string structName)
    {
        BannedEquality.Add(structName);
    }

    public static bool HasBannedEquality(UhtStruct scriptStruct)
    {
        return BannedEquality.Contains(scriptStruct.GetStructName());
    }

    public static void BanArithmetic(string structName)
    {
        BannedArithmetic.Add(structName);
    }

    public static bool HasBannedArithmetic(UhtStruct scriptStruct)
    {
        return BannedArithmetic.Contains(scriptStruct.GetStructName());
    }
}