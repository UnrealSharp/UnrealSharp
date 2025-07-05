using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnrealSharp.SourceGenerators.CodeAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UInterfaceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UInterfacePropertyTypeRule,
        UInterfaceFunctionParameterTypeRule
    );
    
    private static readonly DiagnosticDescriptor UInterfacePropertyTypeRule = new(
        id: "US0003", 
        title: "UnrealSharp UInterface UProperty Analyzer", 
        messageFormat: "{0} is a UProperty with Interface type, which should has UInterface attribute", 
        RuleCategory.Category, 
        DiagnosticSeverity.Error, 
        isEnabledByDefault: true, 
        description: "Ensures UProperty type has a UInterface attribute."
    );
    
    private static readonly DiagnosticDescriptor UInterfaceFunctionParameterTypeRule = new(
        id: "US0004", 
        title: "UnrealSharp UInterface function parameter Analyzer", 
        messageFormat: "{0} is UFunction parameter with Interface type, which should has UInterface attribute", 
        RuleCategory.Category, 
        DiagnosticSeverity.Error, 
        isEnabledByDefault: true, 
        description: "Ensures interface type has a UInterface attribute."
    );

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeFunctionParameter, SymbolKind.Parameter);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol propertySymbol)
        {
            return;
        }

        if (!AnalyzerStatics.HasAttribute(propertySymbol, AnalyzerStatics.UPropertyAttribute))
        {
            return;
        }
        
        var isInterfaceType = propertySymbol.Type.TypeKind == TypeKind.Interface;
        if (!isInterfaceType)
        {
            return;
        }
        
        var hasUInterfaceAttribute = AnalyzerStatics.HasAttribute(propertySymbol.Type, AnalyzerStatics.UInterfaceAttribute);

        if (!hasUInterfaceAttribute && !AnalyzerStatics.IsContainerInterface(propertySymbol.Type))
        {
            context.ReportDiagnostic(Diagnostic.Create(UInterfacePropertyTypeRule, propertySymbol.Locations[0], propertySymbol.Name));
        }
    }

    private static void AnalyzeFunctionParameter(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IParameterSymbol parameterSymbol)
        {
            return;
        }

        var isMethodParameter = parameterSymbol.ContainingSymbol.Kind == SymbolKind.Method;
        if (!isMethodParameter)
        {
            return;
        }
        
        var isUFunction = AnalyzerStatics.HasAttribute(context.Symbol.ContainingSymbol, AnalyzerStatics.UFunctionAttribute);
        if (!isUFunction)
        {
            return;
        }
        
        var isInterfaceType = parameterSymbol.Type.TypeKind == TypeKind.Interface;
        if (!isInterfaceType)
        {
            return;
        }
        
        var hasUInterfaceAttribute = AnalyzerStatics.HasAttribute(parameterSymbol.Type, AnalyzerStatics.UInterfaceAttribute);
        if (!hasUInterfaceAttribute && !AnalyzerStatics.IsContainerInterface(parameterSymbol.Type))
        {
            context.ReportDiagnostic(Diagnostic.Create(UInterfaceFunctionParameterTypeRule, parameterSymbol.Locations[0], parameterSymbol.Name));
        }
    }
}