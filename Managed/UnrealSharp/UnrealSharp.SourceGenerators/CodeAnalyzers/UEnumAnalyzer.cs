using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnrealSharp.SourceGenerators.CodeAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UEnumAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UEnumIsByteEnumRule
    );
    
    private static readonly DiagnosticDescriptor UEnumIsByteEnumRule = new(
        id: "US0002", 
        title: "UnrealSharp UEnumIsByteEnum Analyzer", 
        messageFormat: "{0} is a UEnum, which should have a underlying type of byte", 
        RuleCategory.Category, 
        DiagnosticSeverity.Error, 
        isEnabledByDefault: true, 
        description: "Ensures UEnum underlying type is byte."
    );
    
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSymbolAction(Test, SymbolKind.NamedType);
    }

    private static void Test(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        var isUEnum = namedTypeSymbol.TypeKind == TypeKind.Enum && AnalyzerStatics.HasAttribute(namedTypeSymbol, AnalyzerStatics.UEnumAttribute);
        if (isUEnum && !IsByteEnum(namedTypeSymbol) && !AnalyzerStatics.HasAttribute(namedTypeSymbol, AnalyzerStatics.GeneratedTypeAttribute))
        {
            context.ReportDiagnostic(Diagnostic.Create(UEnumIsByteEnumRule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name));
        }
    }

    private static bool IsByteEnum(INamedTypeSymbol symbol)
    {
        return symbol.EnumUnderlyingType?.SpecialType == SpecialType.System_Byte;
    }
    
}