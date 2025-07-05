using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace UnrealSharp.SourceGenerators.CodeAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UObjectCreationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UObjectCreationRule,
        AActorCreationRule,
        UUserWidgetCreationRule,
        UActorComponentCreationRule,
        USceneComponentCreationRule
    );
    
    private static readonly DiagnosticDescriptor UObjectCreationRule = new(
        id: "US0011", 
        title: "UnrealSharp UObject creation Analyzer", 
        messageFormat: "{0} is a UObject, which should be created by calling the method NewObject<T>()", 
        RuleCategory.Category, 
        DiagnosticSeverity.Error, 
        isEnabledByDefault: true, 
        description: "Ensures UObject instantiated by using NewObject<T>() method."
        );
    
    private static readonly DiagnosticDescriptor AActorCreationRule = new(
        id: "US0010", 
        title: "UnrealSharp AActor creation Analyzer", 
        messageFormat: "{0} is a AActor, which should be created by calling the method SpawnActor<T>()", 
        RuleCategory.Category, 
        DiagnosticSeverity.Error, 
        isEnabledByDefault: true, 
        description: "Ensures AActor instantiated by using SpawnActor<T>() method."
    );
    
    private static readonly DiagnosticDescriptor UUserWidgetCreationRule = new(
        id: "US0009", 
        title: "UnrealSharp UUserWidget creation Analyzer", 
        messageFormat: "{0} is a UUserWidget, which should be created by calling the method CreateWidget<T>()", 
        RuleCategory.Category, 
        DiagnosticSeverity.Error, 
        isEnabledByDefault: true, 
        description: "Ensures UUserWidget instantiated by using CreateWidget<T>() method."
    );
    
    private static readonly DiagnosticDescriptor UActorComponentCreationRule = new(
        id: "US0008", 
        title: "UnrealSharp UActorComponent creation Analyzer", 
        messageFormat: "{0} is a UActorComponent, which should be created by calling the method AddComponentByClass<T>()", 
        RuleCategory.Category, 
        DiagnosticSeverity.Error, 
        isEnabledByDefault: true, 
        description: "Ensures UActorComponent instantiated by using AddComponentByClass<T>() method."
    );
    
    private static readonly DiagnosticDescriptor USceneComponentCreationRule = new(
        id: "US0007", 
        title: "UnrealSharp USceneComponent creation Analyzer", 
        messageFormat: "{0} is a USceneComponent, which should be created by calling the method AddComponentByClass<T>()", 
        RuleCategory.Category, 
        DiagnosticSeverity.Error, 
        isEnabledByDefault: true, 
        description: "Ensures USceneComponent instantiated by using AddComponentByClass<T>() method."
    );
    
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterOperationAction(AnalyzeUObjectCreation, OperationKind.ObjectCreation);
    }

    //check new <UObject> syntax
    private static void AnalyzeUObjectCreation(OperationAnalysisContext context)
    {
        if (context.Operation is not IObjectCreationOperation creationOperation)
        {
            return;
        }

        if (creationOperation.Type is not INamedTypeSymbol type)
        {
            return;
        }

        var isNewKeywordOperation = AnalyzerStatics.IsNewKeywordInstancingOperation(creationOperation, out var newKeywordLocation);
        if (!isNewKeywordOperation) return;

        var rule = GetRule(type);
        if (rule is null) return;
        
        context.ReportDiagnostic(Diagnostic.Create(rule, newKeywordLocation, type.Name));

    }

    private static DiagnosticDescriptor? GetRule(INamedTypeSymbol type)
    {
        return type switch
        {
            _ when AnalyzerStatics.InheritsFrom(type, AnalyzerStatics.USceneComponent) => USceneComponentCreationRule,
            _ when AnalyzerStatics.InheritsFrom(type, AnalyzerStatics.UActorComponent) => UActorComponentCreationRule,
            _ when AnalyzerStatics.InheritsFrom(type, AnalyzerStatics.UUserWidget) => UUserWidgetCreationRule,
            _ when AnalyzerStatics.InheritsFrom(type, AnalyzerStatics.AActor) => AActorCreationRule,
            _ when AnalyzerStatics.InheritsFrom(type, AnalyzerStatics.UObject) => UObjectCreationRule,
            _ => null
        };
    }

}