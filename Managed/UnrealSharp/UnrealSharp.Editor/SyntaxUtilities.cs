using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.Editor.Interop;
using UnrealSharp.SourceGenerator.Utilities;

namespace UnrealSharp.Editor;

[Flags]
// ReSharper disable once InconsistentNaming
public enum ECSTypeStructuralFlags : byte
{
    None = 0,
    StructuralChanges = 1 << 0, 
    ConstructorChanges = 1 << 1,
};

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
            
            ECSTypeStructuralFlags dirtyFlags = ECSTypeStructuralFlags.StructuralChanges;

            if (oldTypeDeclarations != null)
            {
                BaseTypeDeclarationSyntax? oldTypeDecl = oldTypeDeclarations.FirstOrDefault(t => t.Identifier.Text == newTypeDecl.Identifier.Text);

                if (oldTypeDecl != null)
                {
                    if (newTypeDecl.IsEquivalentTo(oldTypeDecl, topLevel: false))
                    {
                        continue;
                    }
                    
                    if (HasConstructorChanged(newTypeDecl, oldTypeDecl))
                    {
                        dirtyFlags |= ECSTypeStructuralFlags.ConstructorChanges;
                    }
                }
            }

            DirtyUnrealType(newTypeDecl, owningProject, dirtyFlags);
        }
    }

    public static void DirtyUnrealType(BaseTypeDeclarationSyntax syntax, Project owningProject, ECSTypeStructuralFlags flags)
    {
        string typeNameSpace = syntax.GetFullNamespace();
        string typeName = syntax.Identifier.Text.Substring(1);
        string assemblyName = owningProject.AssemblyName;
        
        FUnrealSharpEditorModuleExporter.CallDirtyUnrealType(assemblyName, typeNameSpace, typeName, flags);
    }
    
    private static bool HasConstructorChanged(BaseTypeDeclarationSyntax newBaseType, BaseTypeDeclarationSyntax oldBaseType)
    {
        if (newBaseType is not TypeDeclarationSyntax newType || oldBaseType is not TypeDeclarationSyntax oldType)
        {
            return false;
        }
        
        ConstructorDeclarationSyntax? newConstructor = GetConstructor(newType);
        ConstructorDeclarationSyntax? oldConstructor = GetConstructor(oldType);
        
        if (newConstructor == null && oldConstructor == null)
        {
            // No constructors existed before or now
            return false;
        }
        
        if (newConstructor == null || oldConstructor == null)
        {
            // New constructor was added or removed
            return true;
        }
        
        return !newConstructor.IsEquivalentTo(oldConstructor, topLevel: false);
    }

    private static ConstructorDeclarationSyntax? GetConstructor(TypeDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault(ctor => ctor.ParameterList.Parameters.Count == 0);
    }
}