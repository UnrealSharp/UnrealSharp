using Microsoft.CodeAnalysis;

namespace UnrealSharp.SourceGenerators.PrefixHelpers;

public static class PrefixStatics
{
    public const string UStructAttribute = "UStructAttribute";
    public const string UEnumAttribute = "UEnumAttribute";
    public const string UClassAttribute = "UClassAttribute";
    
    public const string GeneratedTypeAttribute = "GeneratedTypeAttribute";
    
    public const string UObject = "UObject";
    public const string AActor = "AActor";
    
    internal static bool HasAttribute(INamedTypeSymbol symbol, string attributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.Name == attributeName)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool InheritsFrom(INamedTypeSymbol symbol, string baseTypeName)
    {
        INamedTypeSymbol currentSymbol = symbol;

        while (currentSymbol != null)
        {
            if (currentSymbol.Name == baseTypeName)
            {
                return true;
            }
            currentSymbol = currentSymbol.BaseType;
        }

        return false;
    }
}