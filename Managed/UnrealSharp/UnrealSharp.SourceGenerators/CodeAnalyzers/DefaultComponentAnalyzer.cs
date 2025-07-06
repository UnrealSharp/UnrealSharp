using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnrealSharp.SourceGenerators.CodeAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DefaultComponentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DefaultComponentRule,
        DefaultComponentSetterRule
        );
    
    public const string DefaultComponentAnalyzerId = "US0013";
    private static readonly LocalizableString DefaultComponentAnalyzerTitle = "UnrealSharp DefaultComponent Analyzer";
    private static readonly LocalizableString DefaultComponentAnalyzerMessageFormat = "{0} is a DefaultComponent, which is not inherit from UActorComponent";
    private static readonly LocalizableString DefaultComponentAnalyzerDescription = "Ensures property type marked as DefaultComponent inherits from UActorComponent.";
    private static readonly DiagnosticDescriptor DefaultComponentRule = new(DefaultComponentAnalyzerId, DefaultComponentAnalyzerTitle, DefaultComponentAnalyzerMessageFormat, RuleCategory.Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DefaultComponentAnalyzerDescription);

    public const string DefaultComponentSetterAnalyzerId = "US0014";
    private static readonly LocalizableString DefaultComponentSetterAnalyzerTitle = "UnrealSharp DefaultComponent Setter Analyzer";
    private static readonly LocalizableString DefaultComponentSetterAnalyzerMessageFormat = "{0} is a DefaultComponent without setter";
    private static readonly LocalizableString DefaultComponentSetterAnalyzerDescription = "Ensures property marked as DefaultComponent has setter.";
    private static readonly DiagnosticDescriptor DefaultComponentSetterRule = new(DefaultComponentSetterAnalyzerId, DefaultComponentSetterAnalyzerTitle, DefaultComponentSetterAnalyzerMessageFormat, RuleCategory.Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DefaultComponentSetterAnalyzerDescription);
    
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSymbolAction(AnalyzeClassProperties, SymbolKind.Property);
    }

    private static void AnalyzeClassProperties(SymbolAnalysisContext context)
    {
        if (context.Symbol.ContainingType.TypeKind != TypeKind.Class)
        {
            return;
        }
        
        if (context.Symbol is not IPropertySymbol propertySymbol)
        {
            return;
        }
        
        if (!AnalyzerStatics.TryGetAttribute(propertySymbol, AnalyzerStatics.UPropertyAttribute, out var propertyAttribute))
        {
            return;
        }

        bool isDefaultComponent = AnalyzerStatics.IsDefaultComponent(propertyAttribute);
        bool inheritFromActorComponent = AnalyzerStatics.InheritsFrom(propertySymbol, AnalyzerStatics.UActorComponent);
        
        if (isDefaultComponent && !inheritFromActorComponent)
        {
            context.ReportDiagnostic(Diagnostic.Create(DefaultComponentRule, propertySymbol.Locations[0], propertySymbol.Name));
        }

        if (isDefaultComponent && propertySymbol.SetMethod is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(DefaultComponentSetterRule, propertySymbol.Locations[0], propertySymbol.Name));
        }
    }

}