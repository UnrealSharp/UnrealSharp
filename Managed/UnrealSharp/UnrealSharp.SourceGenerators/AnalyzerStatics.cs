using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace UnrealSharp.SourceGenerators;

public static class AnalyzerStatics
{
    public const string UStructAttribute = "UStructAttribute";
    public const string UEnumAttribute = "UEnumAttribute";
    public const string UClassAttribute = "UClassAttribute";
    public const string UInterfaceAttribute = "UInterfaceAttribute";
    public const string UMultiDelegateAttribute = "UMultiDelegateAttribute";
    public const string USingleDelegateAttribute = "USingleDelegateAttribute";

    public const string GeneratedTypeAttribute = "GeneratedTypeAttribute";
    
    public const string UPropertyAttribute = "UPropertyAttribute";
    
    public const string BindingAttribute = "BindingAttribute";
    
    public const string UObject = "UObject";
    public const string AActor = "AActor";
    
    public const string DefaultComponent = "DefaultComponent";
    public const string New = "new";
    public const string UActorComponent = "UActorComponent";
    public const string USceneComponent = "USceneComponent";
    public const string UUserWidget = "UUserWidget";
    
    internal static bool HasAttribute(ISymbol symbol, string attributeName)
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

    internal static bool TryGetAttribute(ISymbol symbol, string attributeName, out AttributeData? attribute)
    {
        attribute = symbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass is not null && x.AttributeClass.Name == attributeName);
        
        return attribute is not null;
    }
    
    internal static bool HasAttribute(MemberDeclarationSyntax memberDecl, string attributeName)
    {
        return memberDecl.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .Any(attr => attr.Name.ToString().Contains(attributeName));
    }

    internal static bool InheritsFrom(IPropertySymbol propertySymbol, string baseTypeName)
    {
        return propertySymbol.Type is INamedTypeSymbol namedTypeSymbol && InheritsFrom(namedTypeSymbol, baseTypeName);
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

    internal static bool IsDefaultComponent(AttributeData? attributeData)
    {
        if (attributeData?.AttributeClass?.Name != UPropertyAttribute) return false;

        var argument = attributeData.NamedArguments.FirstOrDefault(x => x.Key == DefaultComponent);
        if (string.IsNullOrWhiteSpace(argument.Key)) return false;

        return argument.Value.Value is true;
    }

    internal static bool IsNewKeywordInstancingOperation(IObjectCreationOperation operation, out Location? location)
    {
        location = null;
        if (operation.Syntax is not ObjectCreationExpressionSyntax objectCreationExpression)
        {
            return false;
        }

        location = objectCreationExpression.NewKeyword.GetLocation();
        return objectCreationExpression.NewKeyword.ValueText == New;
    }
}