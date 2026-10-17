using EpicGames.UHT.Types;
using System.Collections.Generic;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public class InclusionLists
{
    private readonly IDictionary<string, HashSet<string>> BannedProperties = new Dictionary<string, HashSet<string>>();
    private readonly IDictionary<string, HashSet<string>> BannedFunctions = new Dictionary<string, HashSet<string>>();
    private readonly HashSet<string> BannedArithmetic = new();
    private readonly HashSet<string> BannedConstructors = new();

    public void BanProperty(string structName, string propertyName)
    {
        if (!BannedProperties.TryGetValue(structName, out var propertySet))
        {
            propertySet = new HashSet<string>();
            BannedProperties[structName] = propertySet;
        }

        propertySet.Add(propertyName);
    }
    
    public bool HasBannedProperty(UhtProperty property)
    {
        return BannedProperties.TryGetValue(property.Outer!.SourceName, out var propertySet) && propertySet.Contains(property.SourceName);
    }
    
    public void BanFunction(string structName, string functionName)
    {
        if (!BannedFunctions.TryGetValue(structName, out var functionSet))
        {
            functionSet = new HashSet<string>();
            BannedFunctions[structName] = functionSet;
        }

        functionSet.Add(functionName);
    }
    
    public bool HasBannedFunction(UhtFunction function)
    {
        return BannedFunctions.TryGetValue(function.Outer!.SourceName, out HashSet<string>? functionSet) && functionSet.Contains(function.SourceName);
    }

    public void BanArithmetic(string structName)
    {
        BannedArithmetic.Add(structName);
    }

    public bool HasBannedArithmetic(UhtStruct scriptStruct)
    {
        return BannedArithmetic.Contains(scriptStruct.GetStructName());
    }

    public void BanConstructor(string structName)
    {
        BannedConstructors.Add(structName);
    }

    public bool HasBannedConstructor(UhtStruct scriptStruct)
    {
        return BannedConstructors.Contains(scriptStruct.GetStructName());
    }
}