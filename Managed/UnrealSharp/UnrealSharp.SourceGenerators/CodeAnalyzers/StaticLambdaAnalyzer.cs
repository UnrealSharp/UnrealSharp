using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UStaticLambdaAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "US0001",
        title: "Invalid UFunction lambda",
        messageFormat: "Static UFunction lambdas are not supported, since it has no backing UObject instance. Make this lambda an instance method or capture the instance by using instance fields/methods.",
        category: "UnrealSharp",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression);
    }

    private void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        LambdaExpressionSyntax lambda = (LambdaExpressionSyntax) context.Node;
        
        if (lambda.AttributeLists.Count == 0)
        {
            return;
        }

        SemanticModel semanticModel = context.SemanticModel;
        
        bool hasUFunction = lambda.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attr => semanticModel.GetTypeInfo(attr).Type?.Name == "UFunctionAttribute");

        if (!hasUFunction)
        {
            return;
        }
        
        DataFlowAnalysis? dataFlow = semanticModel.AnalyzeDataFlow(lambda);
        if (dataFlow != null && !dataFlow.Captured.Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, lambda.GetLocation()));
        }
    }
}
