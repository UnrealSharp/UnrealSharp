using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.Editor;

internal static class SourceCodeNavigation
{
    public static Location? FindClassDeclaration(Compilation compilation, string typeName)
    {
        // Search the defining assembly, not its references: different projects can contain the same type name.
        INamedTypeSymbol? type = compilation.Assembly.GetTypeByMetadataName(typeName);
        if (type is null)
        {
            return null;
        }

        // A partial class can span several files. Prefer the declaration that exposes it to Unreal.
        foreach (AttributeData attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "UnrealSharp.Attributes.UClassAttribute")
            {
                continue;
            }

            if (attribute.ApplicationSyntaxReference?.GetSyntax().FirstAncestorOrSelf<TypeDeclarationSyntax>() is { } declaration)
            {
                return declaration.Identifier.GetLocation();
            }
        }

        foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is TypeDeclarationSyntax declaration)
            {
                return declaration.Identifier.GetLocation();
            }
        }

        return null;
    }
}
