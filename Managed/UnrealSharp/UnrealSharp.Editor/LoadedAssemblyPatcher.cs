using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using UnrealSharp.Attributes;
using UnrealSharp.Plugins;

namespace UnrealSharp.Editor;

public static class LoadedAssemblyPatcher
{
    private static readonly HashSet<SyntaxKind> NonPlainMethodModifiers =
    [
        SyntaxKind.VirtualKeyword,
        SyntaxKind.AbstractKeyword,
        SyntaxKind.OverrideKeyword,
        SyntaxKind.PartialKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.NewKeyword,
    ];

    private static readonly HashSet<SyntaxKind> SyntaxThatCompilesToHiddenMembers =
    [
        SyntaxKind.SimpleLambdaExpression,
        SyntaxKind.ParenthesizedLambdaExpression,
        SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.LocalFunctionStatement,
        SyntaxKind.AwaitExpression,
        SyntaxKind.YieldReturnStatement,
        SyntaxKind.YieldBreakStatement,
        SyntaxKind.QueryExpression,
        SyntaxKind.AnonymousObjectCreationExpression,
    ];

    private static readonly HashSet<SyntaxKind> SyntaxNewTypesCantHave =
    [
        ..SyntaxThatCompilesToHiddenMembers,
        SyntaxKind.AttributeList,
        SyntaxKind.TypeParameterList,
    ];

    private static readonly HashSet<SyntaxKind> ModifiersNewTypesCantHave =
    [
        SyntaxKind.PartialKeyword,
        SyntaxKind.AsyncKeyword,
    ];

    public static bool TryPatchLoadedAssembly(Project project, GenState state, Compilation newCompilation)
    {
        try
        {
            return PatchLoadedAssembly(project, state, newCompilation);
        }
        catch (Exception exception)
        {
            LogUnrealSharpEditor.LogWarning($"Patching {project.Name} failed, doing a full reload: {exception}");
            return false;
        }
    }

    private static bool PatchLoadedAssembly(Project project, GenState state, Compilation newCompilation)
    {
        List<SemanticEdit>? edits = FindPatchableEdits(state, newCompilation);
        if (edits == null)
        {
            return false;
        }

        if (PluginLoader.FindPlugin(project.AssemblyName)?.Assembly?.Target is not Assembly loadedAssembly)
        {
            LogUnrealSharpEditor.Log($"{project.Name} isn't loaded, doing a full reload.");
            return false;
        }

        HashSet<ISymbol> inserted = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (SemanticEdit edit in edits.Where(edit => edit.Kind == SemanticEditKind.Insert))
        {
            inserted.Add(edit.NewSymbol!);
        }

        using MemoryStream metadata = new MemoryStream();
        using MemoryStream il = new MemoryStream();
        using MemoryStream pdb = new MemoryStream();
        Func<ISymbol, bool> isAdded = symbol => IsInserted(symbol, inserted);
        EmitBaseline baseline = state.LoadedBaseline!;
        EmitDifferenceResult patch = newCompilation.EmitDifference(baseline, edits, isAdded, metadata, il, pdb);

        if (!patch.Success)
        {
            string errors = IncrementalCompilationManager.FormatErrors(patch.Diagnostics);
            LogUnrealSharpEditor.Log($"Couldn't build a patch for {project.Name}, doing a full reload: {errors}");
            return false;
        }

        try
        {
            MetadataUpdater.ApplyUpdate(loadedAssembly, metadata.ToArray(), il.ToArray(), pdb.ToArray());
        }
        catch (Exception exception)
        {
            LogUnrealSharpEditor.Log($"The runtime refused the patch, doing a full reload: {exception.Message}");
            return false;
        }

        state.MarkLoaded(newCompilation, patch.Baseline!);
        LogUnrealSharpEditor.Log($"Patched {project.Name} in place, no full reload needed.");
        return true;
    }

    private static List<SemanticEdit>? FindPatchableEdits(GenState state, Compilation newCompilation)
    {
        if (state.LoadedBaseline == null)
        {
            LogUnrealSharpEditor.Log("First C# change since startup, doing a full reload so later ones can be patched.");
            return null;
        }

        List<SemanticEdit> edits = new List<SemanticEdit>();
        if (!state.ChangedSinceLoad.All(path => TryAddEditsForPath(edits, state, newCompilation, path)))
        {
            return null;
        }

        return edits;
    }

    private static bool TryAddEditsForPath(List<SemanticEdit> edits, GenState state, Compilation compilation, string path)
    {
        string fileName = Path.GetFileName(path);
        if (!state.TreesByPath!.TryGetValue(path, out SyntaxTree? newTree))
        {
            LogUnrealSharpEditor.Log($"{fileName} was deleted, doing a full reload.");
            return false;
        }

        SemanticModel newModel = compilation.GetSemanticModel(newTree);
        if (!state.LoadedTreesByPath.TryGetValue(path, out SyntaxTree? loadedTree))
        {
            if (TryAddEditsForNewFile(edits, newModel))
            {
                return true;
            }

            LogUnrealSharpEditor.Log($"{fileName} is a new file with more than plain C# types, doing a full reload.");
            return false;
        }

        SemanticModel loadedModel = state.LoadedCompilation!.GetSemanticModel(loadedTree);
        if (!new ChangedFile(edits, loadedModel, newModel).TryAddEdits())
        {
            LogUnrealSharpEditor.Log($"{fileName} has changes that can't be patched in place, doing a full reload.");
            return false;
        }

        return true;
    }

    private static bool TryAddEditsForNewFile(List<SemanticEdit> edits, SemanticModel newModel)
    {
        CompilationUnitSyntax root = (CompilationUnitSyntax)newModel.SyntaxTree.GetRoot();
        if (root.AttributeLists.Count > 0 || root.Usings.Any(directive => directive.GlobalKeyword != default))
        {
            return false;
        }

        return TryInsertTypes(edits, newModel, root.Members.SelectMany(GetMembersOutsideNamespaces));
    }

    private static IEnumerable<MemberDeclarationSyntax> GetMembersOutsideNamespaces(MemberDeclarationSyntax member)
    {
        if (member is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
        {
            return [member];
        }

        return namespaceDeclaration.Members.SelectMany(GetMembersOutsideNamespaces);
    }

    private static HashSet<SyntaxNode> FindAddedTypes(SyntaxTree loadedTree, SyntaxTree newTree)
    {
        IEnumerable<string> loadedPaths = TypesIn(loadedTree).Select(GetTypePath);
        List<BaseTypeDeclarationSyntax> added = TypesIn(newTree).ExceptBy(loadedPaths, GetTypePath).ToList();
        return added.Where(type => !type.Ancestors().Any(added.Contains)).ToHashSet<SyntaxNode>();
    }

    private static IEnumerable<BaseTypeDeclarationSyntax> TypesIn(SyntaxTree tree)
    {
        return tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>();
    }

    private static bool TryInsertTypes(List<SemanticEdit> edits, SemanticModel newModel, IEnumerable<SyntaxNode> types)
    {
        if (!types.All(IsPlainType))
        {
            return false;
        }

        foreach (SyntaxNode type in types)
        {
            edits.Add(new(SemanticEditKind.Insert, null, newModel.GetDeclaredSymbol(type)));
        }

        return true;
    }

    private static bool IsPlainType(SyntaxNode node)
    {
        if (node is not BaseTypeDeclarationSyntax)
        {
            return false;
        }

        bool hasBlockedSyntax = node.DescendantNodesAndSelf().Any(child => SyntaxNewTypesCantHave.Contains(child.Kind()));
        return !hasBlockedSyntax && !node.DescendantTokens().Any(token => ModifiersNewTypesCantHave.Contains(token.Kind()));
    }

    private static string GetTypePath(BaseTypeDeclarationSyntax type)
    {
        return string.Join(".", type.AncestorsAndSelf().Reverse().Select(GetDeclaredName).OfType<string>());
    }

    private static string? GetDeclaredName(SyntaxNode node)
    {
        return node switch
        {
            BaseNamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Name.ToString(),
            BaseTypeDeclarationSyntax typeDeclaration => typeDeclaration.Identifier.Text,
            _ => null,
        };
    }

    private static bool IsInserted(ISymbol symbol, HashSet<ISymbol> inserted)
    {
        return inserted.Contains(symbol) || (symbol.ContainingType is { } outer && IsInserted(outer, inserted));
    }

    private static FileMembers ReadMembers(SyntaxTree tree, HashSet<SyntaxNode> skippedTypes)
    {
        List<SyntaxNode> memberBodies = [];
        Dictionary<string, SyntaxNode> methodBodies = new();
        foreach (SyntaxNode node in tree.GetRoot().DescendantNodes(child => !skippedTypes.Contains(child)))
        {
            if (IsPlainMethod(node))
            {
                MethodDeclarationSyntax method = (MethodDeclarationSyntax)node;
                methodBodies.Add(GetDeclarationKey(method), (SyntaxNode?)method.Body ?? method.ExpressionBody!);
                continue;
            }

            if (IsMemberBody(node) && !IsPlainMethod(node.Parent!))
            {
                memberBodies.Add(node);
            }
        }

        return new FileMembers(memberBodies, methodBodies);
    }

    private static string GetDeclarationKey(MethodDeclarationSyntax method)
    {
        BaseTypeDeclarationSyntax containingType = method.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>()!;
        MethodDeclarationSyntax signature = method.WithBody(null).WithExpressionBody(null);
        IEnumerable<string> tokens = signature.DescendantTokens().Select(token => token.Text);
        return GetTypePath(containingType) + ":" + string.Join(" ", tokens);
    }

    private static bool IsPlainMethod(SyntaxNode node)
    {
        if (node is not MethodDeclarationSyntax { TypeParameterList: null, ExplicitInterfaceSpecifier: null } method)
        {
            return false;
        }

        if (method.Parent is not (ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax))
        {
            return false;
        }

        if (method.AttributeLists.Count > 0)
        {
            return false;
        }

        return !method.Modifiers.Any(modifier => NonPlainMethodModifiers.Contains(modifier.Kind()));
    }

    private static bool IsMemberBody(SyntaxNode node)
    {
        if (node is not (BlockSyntax or ArrowExpressionClauseSyntax))
        {
            return false;
        }

        return node.Parent is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or BasePropertyDeclarationSyntax;
    }

    private static bool CompilesToHiddenMembers(SyntaxNode body)
    {
        if (body.Parent is MethodDeclarationSyntax method && method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            return true;
        }

        return body.DescendantNodes().Any(node => SyntaxThatCompilesToHiddenMembers.Contains(node.Kind()));
    }

    private static ISymbol OwnerOf(SemanticModel model, SyntaxNode body)
    {
        if (body.Parent is BasePropertyDeclarationSyntax property)
        {
            return ((IPropertySymbol)model.GetDeclaredSymbol(property)!).GetMethod!;
        }

        return model.GetDeclaredSymbol(body.Parent!)!;
    }

    private static List<IMethodSymbol> MethodsOf(SemanticModel model, List<SyntaxNode> bodies)
    {
        return bodies.Select(body => (IMethodSymbol)OwnerOf(model, body)).ToList();
    }

    private static string GetQualifiedName(IMethodSymbol method)
    {
        return method.ContainingType.ToDisplayString() + "." + method.Name;
    }

    private static bool IsSafeSignatureChange(List<IMethodSymbol> removedWithName, List<IMethodSymbol> addedWithName)
    {
        if (removedWithName.Count != 1 || addedWithName.Count != 1)
        {
            return false;
        }

        ImmutableArray<IParameterSymbol> oldParameters = removedWithName[0].Parameters;
        ImmutableArray<IParameterSymbol> newParameters = addedWithName[0].Parameters;
        if (oldParameters.Concat(newParameters).Any(parameter => parameter.IsParams))
        {
            return false;
        }

        int oldRequired = oldParameters.Count(parameter => !parameter.IsOptional);
        int newRequired = newParameters.Count(parameter => !parameter.IsOptional);
        return newRequired > oldParameters.Length || newParameters.Length < oldRequired;
    }

    private static bool IsModule(INamedTypeSymbol type)
    {
        string moduleAttribute = typeof(UModuleAttribute).FullName!;
        return type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == moduleAttribute);
    }

    private static bool HasOtherMemberWithName(INamedTypeSymbol type, string name, List<IMethodSymbol> added)
    {
        bool hasOther = type.GetMembers(name).Any(member => !added.Contains(member, SymbolEqualityComparer.Default));
        return hasOther || (type.BaseType is { } baseType && HasOtherMemberWithName(baseType, name, added));
    }

    private sealed record FileMembers(List<SyntaxNode> MemberBodies, Dictionary<string, SyntaxNode> MethodBodies)
    {
        public List<SyntaxNode> MethodBodiesMissingFrom(FileMembers other)
        {
            return MethodBodies.Keys.Except(other.MethodBodies.Keys).Select(key => MethodBodies[key]).ToList();
        }
    }

    private sealed class ChangedFile(List<SemanticEdit> edits, SemanticModel loadedModel, SemanticModel newModel)
    {
        public bool TryAddEdits()
        {
            HashSet<SyntaxNode> addedTypes = FindAddedTypes(loadedModel.SyntaxTree, newModel.SyntaxTree);
            SyntaxNode newRoot = newModel.SyntaxTree.GetRoot().RemoveNodes(addedTypes, SyntaxRemoveOptions.KeepNoTrivia)!;
            DeclarationShapeRewriter shape = new DeclarationShapeRewriter();
            bool keepsShape = shape.Visit(loadedModel.SyntaxTree.GetRoot())!.IsEquivalentTo(shape.Visit(newRoot));
            if (!keepsShape || !TryInsertTypes(edits, newModel, addedTypes))
            {
                return false;
            }

            FileMembers before = ReadMembers(loadedModel.SyntaxTree, []);
            FileMembers after = ReadMembers(newModel.SyntaxTree, addedTypes);
            IEnumerable<string> keptMethods = after.MethodBodies.Keys.Intersect(before.MethodBodies.Keys);
            List<SyntaxNode> addedBodies = after.MethodBodiesMissingFrom(before);

            bool bodiesPatch = before.MemberBodies.Zip(after.MemberBodies, TryUpdateBody).All(patched => patched);
            bool methodsPatch = keptMethods.All(key => TryUpdateBody(before.MethodBodies[key], after.MethodBodies[key]));
            if (!bodiesPatch || !methodsPatch || addedBodies.Any(CompilesToHiddenMembers))
            {
                return false;
            }

            List<IMethodSymbol> removed = MethodsOf(loadedModel, before.MethodBodiesMissingFrom(after));
            List<IMethodSymbol> added = MethodsOf(newModel, addedBodies);
            ILookup<string, IMethodSymbol> removedByName = removed.ToLookup(GetQualifiedName);
            ILookup<string, IMethodSymbol> addedByName = added.ToLookup(GetQualifiedName);
            IEnumerable<string> changedNames = removed.Concat(added).Select(GetQualifiedName).Distinct();
            return changedNames.All(name => TryAddSameNameChange(removedByName[name].ToList(), addedByName[name].ToList()));
        }

        private bool TryUpdateBody(SyntaxNode loadedBody, SyntaxNode newBody)
        {
            if (loadedBody.IsEquivalentTo(newBody))
            {
                return true;
            }

            ISymbol loadedOwner = OwnerOf(loadedModel, loadedBody);
            bool touchesHiddenMembers = CompilesToHiddenMembers(loadedBody) || CompilesToHiddenMembers(newBody);
            bool runsOnlyAtLoad = loadedBody.Parent is ConstructorDeclarationSyntax || IsModule(loadedOwner.ContainingType);
            if (runsOnlyAtLoad || touchesHiddenMembers)
            {
                return false;
            }

            edits.Add(new(SemanticEditKind.Update, loadedOwner, OwnerOf(newModel, newBody)));
            return true;
        }

        private bool TryAddSameNameChange(List<IMethodSymbol> removedWithName, List<IMethodSymbol> addedWithName)
        {
            bool isSignatureChange = removedWithName.Count > 0 && addedWithName.Count > 0;
            if (isSignatureChange && !IsSafeSignatureChange(removedWithName, addedWithName))
            {
                return false;
            }

            IMethodSymbol anyWithName = removedWithName.FirstOrDefault() ?? addedWithName[0];
            INamedTypeSymbol newType = FindInNewCompilation(anyWithName.ContainingType);
            if (HasOtherMemberWithName(newType, anyWithName.Name, addedWithName))
            {
                return false;
            }

            edits.AddRange(removedWithName.Select(method => new SemanticEdit(SemanticEditKind.Delete, method, newType)));
            edits.AddRange(addedWithName.Select(method => new SemanticEdit(SemanticEditKind.Insert, null, method)));
            return true;
        }

        private INamedTypeSymbol FindInNewCompilation(INamedTypeSymbol type)
        {
            string typeName = type.ToDisplayString();
            IEnumerable<ISymbol> typesWithName = newModel.Compilation.GetSymbolsWithName(type.Name, SymbolFilter.Type);
            return typesWithName.OfType<INamedTypeSymbol>().First(candidate => candidate.ToDisplayString() == typeName);
        }
    }

    private sealed class DeclarationShapeRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node == null || IsPlainMethod(node))
            {
                return null;
            }

            if (!IsMemberBody(node))
            {
                return base.Visit(node);
            }

            if (node is BlockSyntax)
            {
                return SyntaxFactory.Block();
            }

            return SyntaxFactory.ArrowExpressionClause(SyntaxFactory.IdentifierName("_"));
        }
    }
}
