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
        BaseNamespaceDeclarationSyntax? namespaceNode = declaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();

        if (namespaceNode == null)
        {
            return string.Empty;
        }
        
        StringBuilder namespaceBuilder = new StringBuilder();
        
        namespaceBuilder.Append(namespaceNode.Name);
        BaseNamespaceDeclarationSyntax? currentNamespace = namespaceNode.Parent as BaseNamespaceDeclarationSyntax;
        
        while (currentNamespace != null)
        {
            namespaceBuilder.Insert(0, currentNamespace.Name + ".");
            currentNamespace = currentNamespace.Parent as BaseNamespaceDeclarationSyntax;
        }

        return namespaceBuilder.ToString();
    }
}