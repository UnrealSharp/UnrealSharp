using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using UnrealSharp.SourceGenerator.Utilities;

namespace UnrealSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UPropertyReferenceTypeSuppressor : DiagnosticSuppressor
{
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [DefaultComponentNullableRule];
    
    private static readonly SuppressionDescriptor DefaultComponentNullableRule = new(
        "NullableUPropertySuppressorId",
        "CS8618",
        "UProperties on UClasses are automatically instantiated."
    );
    
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var location = diagnostic.Location;
            var syntaxTree = location.SourceTree;
            if (syntaxTree is null) continue;
            
            var root = syntaxTree.GetRoot(context.CancellationToken);
            var textSpan = location.SourceSpan;
            var node = root.FindNode(textSpan);
            
            if (node is not PropertyDeclarationSyntax propertyNode || propertyNode.AttributeLists.Count == 0) continue;

            var semanticModel = context.GetSemanticModel(syntaxTree);

            if (IsDefaultComponentProperty(semanticModel, propertyNode, context.CancellationToken))
            {
                context.ReportSuppression(Suppression.Create(DefaultComponentNullableRule, diagnostic));
            }
        }
    }

    private static bool IsDefaultComponentProperty(
        SemanticModel semanticModel, 
        PropertyDeclarationSyntax propertyNode,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetDeclaredSymbol(propertyNode, cancellationToken);
        if (symbol is not IPropertySymbol propertySymbol) return false;
        
        return AnalyzerStatics.TryGetAttribute(propertySymbol, AnalyzerStatics.UPropertyAttribute, out _) && 
               AnalyzerStatics.HasAttribute(propertySymbol.ContainingType, AnalyzerStatics.UClassAttribute);
    }
}