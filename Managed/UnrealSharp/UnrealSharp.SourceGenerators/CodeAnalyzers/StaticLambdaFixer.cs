using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace UnrealSharp.SourceGenerators.CodeAnalyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UFunctionLambdaCodeFixProvider)), Shared]
public class UFunctionLambdaCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("US0001");
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Diagnostic diagnostic = context.Diagnostics.First();
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root?.FindNode(diagnostic.Location.SourceSpan) is not LambdaExpressionSyntax lambda)
        {
            return;
        }
        
        context.RegisterCodeFix(Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
            title: "Convert Static Lambda to Instance Method",
            createChangedDocument: c => ConvertToInstanceMethod(context.Document, lambda, c),
            equivalenceKey: "ConvertToInstanceMethod"), diagnostic);
    }

    private async Task<Document> ConvertToInstanceMethod(Document document, LambdaExpressionSyntax lambda, CancellationToken cancellationToken)
    {
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        DocumentEditor? editor = await DocumentEditor.CreateAsync(document, cancellationToken);

        MethodDeclarationSyntax? containingMethod = lambda.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        ClassDeclarationSyntax? containingClass = lambda.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        
        if (containingMethod == null || containingClass == null || semanticModel == null)
        {
            return document;
        }
        
        BlockSyntax lambdaBody = lambda.Body as BlockSyntax
                                 ?? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body));
        
        SeparatedSyntaxList<ParameterSyntax> parameters = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => SyntaxFactory.SingletonSeparatedList(WithInferredType(simple.Parameter, semanticModel, cancellationToken)),
            ParenthesizedLambdaExpressionSyntax paren => SyntaxFactory.SeparatedList(paren.ParameterList.Parameters.Select(p => WithInferredType(p, semanticModel, cancellationToken))),
            _ => default
        };

        string methodName = AnalyzerStatics.GenerateUniqueMethodName(containingClass, "InstanceMethod");
        editor.ReplaceNode(lambda, SyntaxFactory.IdentifierName(methodName));

        MethodDeclarationSyntax methodDecl = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), methodName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
            .WithAttributeLists(lambda.AttributeLists)
            .WithParameterList(SyntaxFactory.ParameterList(parameters))
            .WithBody(lambdaBody);

        editor.AddMember(containingClass, methodDecl);

        Document? changedDocument = editor.GetChangedDocument();
        return await Formatter.FormatAsync(changedDocument, cancellationToken: cancellationToken);
    }

    private static ParameterSyntax WithInferredType(ParameterSyntax parameter, SemanticModel model, CancellationToken token)
    {
        IParameterSymbol? symbol = model.GetDeclaredSymbol(parameter, token);
        
        if (symbol == null)
        {
            return parameter.WithType(SyntaxFactory.IdentifierName("object"));
        }

        SymbolDisplayFormat displayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
        );

        string typeName = symbol.Type.ToDisplayString(displayFormat);
        return parameter.WithType(SyntaxFactory.ParseTypeName(typeName));
    }
}