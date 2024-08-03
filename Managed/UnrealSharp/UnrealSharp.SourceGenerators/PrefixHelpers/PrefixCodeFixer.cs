using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators.PrefixHelpers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrefixAnalyzerCodeFixProvider)), Shared]
public class PrefixAnalyzerCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add prefix";

    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnrealTypeAnalyzer.PrefixAnalyzerId);

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return; 
        }

        Diagnostic diagnostic = context.Diagnostics[0];
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        TypeDeclarationSyntax declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

        context.RegisterCodeFix(
            CodeAction.Create(Title, 
                c => AddPrefixAsync(context.Document, declaration, c), Title), 
            diagnostic);
    }

    private async Task<Solution> AddPrefixAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        var typeSymbol = ModelExtensions.GetDeclaredSymbol(semanticModel, typeDecl, cancellationToken) as INamedTypeSymbol;
        
        string prefix = "";
        Console.WriteLine(typeSymbol.TypeKind);
        if (typeSymbol.TypeKind == TypeKind.Struct)
        {
            prefix = "F";
        }
        else if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            prefix = "E";
        }
        else if (typeSymbol.TypeKind == TypeKind.Class)
        {
            if (PrefixStatics.InheritsFrom(typeSymbol, PrefixStatics.AActor))
            {
                prefix = "A";
            }
            else if (PrefixStatics.InheritsFrom(typeSymbol, PrefixStatics.UObject))
            {
                prefix = "U";
            }
        }
        
        if (string.IsNullOrEmpty(prefix))
        {
            return document.Project.Solution;
        }

        string newName = prefix + typeDecl.Identifier.Text;
        Solution solution = document.Project.Solution;
        return await RenameSymbolAsync(solution, typeDecl, newName, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Solution> RenameSymbolAsync(Solution solution, TypeDeclarationSyntax typeDecl, string newName, CancellationToken cancellationToken)
    {
        var semanticModel = await solution.GetDocument(typeDecl.SyntaxTree).GetSemanticModelAsync(cancellationToken);
        var symbol = ModelExtensions.GetDeclaredSymbol(semanticModel, typeDecl, cancellationToken);
        var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
        return newSolution;
    }
}