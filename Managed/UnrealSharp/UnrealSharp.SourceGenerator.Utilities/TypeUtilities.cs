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
        SyntaxNode? parent = declaration.Parent;

        while (parent != null)
        {
            if (parent is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                return namespaceDeclaration.Name.ToString();
            }

            parent = parent.Parent;
        }

        return string.Empty;
    }
}