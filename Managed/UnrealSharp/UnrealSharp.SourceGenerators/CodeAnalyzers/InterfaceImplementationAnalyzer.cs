using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnrealSharp.SourceGenerators.CodeAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UFunctionConflictAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "US0012",
        "Conflicting UFunction Attribute",
        "Method '{0}' in class '{1}' should not have a UFunction attribute because it is already defined in the interface '{2}'",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        IMethodSymbol methodSymbol = (IMethodSymbol) context.Symbol;
        
        foreach (var implementedMethod in methodSymbol.ExplicitInterfaceImplementations)
        {
            CheckForUFunctionConflict(context, methodSymbol, implementedMethod);
        }
        
        foreach (INamedTypeSymbol? typeSymbol in methodSymbol.ContainingType.AllInterfaces)
        {
            foreach (IMethodSymbol? interfaceMethod in typeSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                ISymbol? implementation = methodSymbol.ContainingType.FindImplementationForInterfaceMember(interfaceMethod);
                if (implementation?.Equals(methodSymbol) == true)
                {
                    CheckForUFunctionConflict(context, methodSymbol, interfaceMethod);
                }
            }
        }
    }

    private static void CheckForUFunctionConflict(
        SymbolAnalysisContext context,
        IMethodSymbol implementationMethod,
        IMethodSymbol interfaceMethod)
    {
        bool HasUFunction(IMethodSymbol method)
        {
            return method.GetAttributes().Any(attr =>
                attr.AttributeClass?.Name == AnalyzerStatics.UFunctionAttribute);
        }
        
        if (!HasUFunction(interfaceMethod) || !HasUFunction(implementationMethod))
        {
            return;
        }
        
        Diagnostic diagnostic = Diagnostic.Create(
            Rule,
            implementationMethod.Locations[0],
            implementationMethod.Name,
            implementationMethod.ContainingType.Name,
            interfaceMethod.ContainingType.Name);

        context.ReportDiagnostic(diagnostic);
    }
}