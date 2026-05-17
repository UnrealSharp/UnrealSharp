using System.Collections.Generic;
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
using UnrealSharp.Analyzers;

namespace UnrealSharp.CodeFixer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnrealTypeCodeFixProvider)), Shared]
public class UnrealTypeCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        UnrealTypeAnalyzer.StructAnalyzerId,
        UnrealTypeAnalyzer.ClassAnalyzerId,
        UnrealTypeAnalyzer.PrefixAnalyzerId
    ];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        
        if (root == null)
        {
            return;
        }

        foreach (Diagnostic? diagnostic in context.Diagnostics)
        {
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
            SyntaxNode node = root.FindNode(diagnosticSpan);
 
            switch (diagnostic.Id)
            {
                case UnrealTypeAnalyzer.StructAnalyzerId:
                    if (node is PropertyDeclarationSyntax propertyNode)
                    {
                        string name = propertyNode.Identifier.Text;
                        PropertyDeclarationSyntax propertyNodeCopy = propertyNode;
                        
                        context.RegisterCodeFix(
                            CodeAction.Create(title: $"Convert '{name}' to field",
                                createChangedDocument: c => ConvertPropertyToFieldAsync(context.Document, propertyNodeCopy, c),
                                equivalenceKey: "ConvertToField"),
                            diagnostic);
                    }
                    break;

                case UnrealTypeAnalyzer.ClassAnalyzerId:
                    if (node is VariableDeclaratorSyntax fieldNode)
                    {
                        string name = fieldNode.Identifier.Text;
                        var fieldNodeCopy = fieldNode;
                        
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: $"Convert '{name}' to property",
                                createChangedDocument: cancellationToken => ConvertFieldToPropertyAsync(context.Document, fieldNodeCopy, cancellationToken),
                                equivalenceKey: "ConvertToProperty"),
                            diagnostic);
                    }
                    break;

                case UnrealTypeAnalyzer.PrefixAnalyzerId:
                    if (node is BaseTypeDeclarationSyntax declaration)
                    {
                        string? prefix = diagnostic.Properties["Prefix"];
                        
                        if (prefix == null)
                        {
                            break;
                        }

                        BaseTypeDeclarationSyntax declarationCopy = declaration;
                        
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: $"Add prefix '{prefix}' to '{declaration.Identifier.Text}'",
                                createChangedDocument: c => AddPrefixToDeclarationAsync(context.Document, declarationCopy, prefix, c),
                                equivalenceKey: "AddPrefix"),
                            diagnostic);
                    }
                    break;
            }
        }
    }

    private async Task<Document> ConvertPropertyToFieldAsync(Document document, PropertyDeclarationSyntax propertyDeclaration, CancellationToken cancellationToken)
    {
        VariableDeclarationSyntax variableDeclaration = SyntaxFactory.VariableDeclaration(propertyDeclaration.Type)
            .AddVariables(SyntaxFactory.VariableDeclarator(propertyDeclaration.Identifier));

        FieldDeclarationSyntax fieldDeclaration = SyntaxFactory.FieldDeclaration(variableDeclaration)
            .WithModifiers(propertyDeclaration.Modifiers)
            .WithAttributeLists(propertyDeclaration.AttributeLists)
            .WithTriviaFrom(propertyDeclaration);
        
        SyntaxTriviaList leadingTrivia = propertyDeclaration.GetLeadingTrivia();
        SyntaxTriviaList trailingTrivia = propertyDeclaration.GetTrailingTrivia();
        
        if (leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            IEnumerable<SyntaxTrivia> newLeadingTrivia = leadingTrivia.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));
            fieldDeclaration = fieldDeclaration.WithLeadingTrivia(newLeadingTrivia);
        }

        fieldDeclaration = fieldDeclaration.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);

        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
        
        if (root == null)
        {
            return document;
        }
        
        SyntaxNode newRoot = root.ReplaceNode(propertyDeclaration, fieldDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
    
    private async Task<Document> ConvertFieldToPropertyAsync(Document document, VariableDeclaratorSyntax fieldDeclaration, CancellationToken cancellationToken)
    {
        if (fieldDeclaration.Parent is not VariableDeclarationSyntax parentFieldDeclaration)
        {
            return document;
        }

        if (parentFieldDeclaration.Parent is not FieldDeclarationSyntax fieldDecl)
        {
            return document;
        }

        PropertyDeclarationSyntax propertyDeclaration = SyntaxFactory.PropertyDeclaration(parentFieldDeclaration.Type, fieldDeclaration.Identifier)
            .AddModifiers(fieldDecl.Modifiers.ToArray())
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
            {
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            })))
            .WithTriviaFrom(fieldDecl)
            .WithAttributeLists(fieldDecl.AttributeLists);

        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
        
        if (root == null)
        {
            return document;
        }
        
        SyntaxNode newRoot = root.ReplaceNode(fieldDecl, propertyDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }

    private async Task<Document> AddPrefixToDeclarationAsync(Document document, BaseTypeDeclarationSyntax declaration, string prefix, CancellationToken cancellationToken)
    {
        SyntaxToken identifierToken = declaration.Identifier;
        string newName = prefix + identifierToken.Text;
        SyntaxToken newIdentifierToken = SyntaxFactory.Identifier(newName);
        
        MemberDeclarationSyntax newDeclaration = declaration.WithIdentifier(newIdentifierToken).WithTriviaFrom(declaration);
        
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        
        if (root == null)
        {
            return document;
        }
        
        SyntaxNode newRoot = root.ReplaceNode(declaration, newDeclaration);
        
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        ISymbol? symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
        
        if (symbol == null)
        {
            return document.WithSyntaxRoot(newRoot);
        }
        
        Solution solution = document.Project.Solution;
        
        SymbolRenameOptions options = new SymbolRenameOptions();
        Solution newSolution = await Renamer.RenameSymbolAsync(solution, symbol, options, newName, cancellationToken);
        
        return newSolution.GetDocument(document.Id)!;
    }
}