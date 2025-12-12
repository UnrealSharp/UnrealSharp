using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.Editor.Interop;
using UnrealSharp.SourceGenerator.Utilities;

namespace UnrealSharp.Editor;

public static class SyntaxUtilities
{
    public static void LookForChangesInUnrealTypes(SyntaxTree newTree, SyntaxTree? existingTree, Project owningProject)
    {
        List<BaseTypeDeclarationSyntax> newTypeDeclarations = newTree.GetRoot()
            .DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .ToList();

        List<BaseTypeDeclarationSyntax>? oldTypeDeclarations = existingTree != null
            ? existingTree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().ToList()
            : null;

        for (int i = 0; i < newTypeDeclarations.Count; i++)
        {
            BaseTypeDeclarationSyntax newTypeDecl = newTypeDeclarations[i];
            
            if (!newTypeDecl.HasAnyUAttribute())
            {
                continue;
            }

            if (oldTypeDeclarations != null)
            {
                BaseTypeDeclarationSyntax? oldTypeDecl = oldTypeDeclarations.FirstOrDefault(t => t.Identifier.Text == newTypeDecl.Identifier.Text);

                if (oldTypeDecl != null)
                {
                    if (newTypeDecl.IsEquivalentTo(oldTypeDecl, false))
                    {
                        continue;
                    }
                }
            }

            DirtyUnrealType(newTypeDecl, owningProject);
        }
    }

    public static void DirtyUnrealType(BaseTypeDeclarationSyntax syntax, Project owningProject)
    {
        string typeNameSpace = syntax.GetFullNamespace();
        string typeName = syntax.Identifier.Text.Substring(1);
        string assemblyName = owningProject.AssemblyName;
        
        FUnrealSharpEditorModuleExporter.CallDirtyUnrealType(assemblyName, typeNameSpace, typeName);
    }
}