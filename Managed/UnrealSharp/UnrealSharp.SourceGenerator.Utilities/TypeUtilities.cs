using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.SourceGenerator.Utilities;

public static class TypeUtilities
{
    public static bool InheritsFrom(this ITypeSymbol typeSymbol, string baseTypeFullName)
    {
        ITypeSymbol? currentType = typeSymbol;

        while (currentType != null)
        {
            if (currentType.Name == baseTypeFullName)
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }
    
    public static string GetFullNamespace(this CSharpSyntaxNode declaration)
    {
        var namespaceNode = declaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
        var namespaceBuilder = new StringBuilder();
        if (namespaceNode != null)
        {
            namespaceBuilder.Append(namespaceNode.Name.ToString());
            var currentNamespace = namespaceNode.Parent as BaseNamespaceDeclarationSyntax;
            while (currentNamespace != null)
            {
                namespaceBuilder.Insert(0, $"{currentNamespace.Name}.");
                currentNamespace = currentNamespace.Parent as BaseNamespaceDeclarationSyntax;
            }
        }

        return namespaceBuilder.ToString();
    }
    
    public static string? GetAnnotatedTypeName(this TypeSyntax? type, SemanticModel model)
    {
        if (type is null)
        {
            return null;
        }

        ITypeSymbol? typeInfo = model.GetTypeInfo(type).Type;
        return type is NullableTypeSyntax ? typeInfo?.WithNullableAnnotation(NullableAnnotation.Annotated).ToString() : typeInfo?.ToString();
    }
}