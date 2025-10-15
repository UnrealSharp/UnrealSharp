﻿using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using UnrealSharp.SourceGenerator.Utilities;

namespace UnrealSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnrealTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        PrefixRule, 
        ClassRule
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeClassFields, SymbolKind.Field);
    }
    
    public const string StructAnalyzerId = "US0005";
    public const string ClassAnalyzerId = "US0006";
    private static readonly LocalizableString ClassAnalyzerTitle = "UnrealSharp Class Field Analyzer";
    private static readonly LocalizableString ClassAnalyzerMessageFormat = "{0} is a UProperty and a field, which is not allowed in classes. UProperties in classes must be properties.";
    private static readonly LocalizableString ClassAnalyzerDescription = "Ensures UProperties in classes are properties.";
    
    private static readonly DiagnosticDescriptor ClassRule = new(ClassAnalyzerId, ClassAnalyzerTitle, ClassAnalyzerMessageFormat, RuleCategory.Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: ClassAnalyzerDescription);

    private static void AnalyzeFields(SymbolAnalysisContext context, TypeKind typeKind, string requiredAttribute, DiagnosticDescriptor rule)
    {
        ISymbol symbol = context.Symbol;
        INamedTypeSymbol type = symbol.ContainingType;

        if (type.TypeKind != typeKind && !AnalyzerStatics.HasAttribute(type, requiredAttribute))
        {
            return;
        }

        if (!AnalyzerStatics.HasAttribute(symbol, AnalyzerStatics.UPropertyAttribute))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(rule, symbol.Locations[0], symbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeClassFields(SymbolAnalysisContext context)
    {
        if (context.Symbol is IFieldSymbol)
        {
            AnalyzeFields(context, TypeKind.Class, AnalyzerStatics.UClassAttribute, ClassRule);
        }
    }
    
    public const string PrefixAnalyzerId = "PrefixAnalyzer";
    private static readonly LocalizableString PrefixAnalyzerTitle = "UnrealSharp Prefix Analyzer";
    private static readonly LocalizableString PrefixAnalyzerMessageFormat = "{0} '{1}' is exposed to Unreal Engine and should have prefix '{2}'";
    private static readonly LocalizableString PrefixAnalyzerDescription = "Ensures types have appropriate prefixes.";
    private static readonly DiagnosticDescriptor PrefixRule = new(PrefixAnalyzerId, PrefixAnalyzerTitle, PrefixAnalyzerMessageFormat, RuleCategory.Naming, DiagnosticSeverity.Error, isEnabledByDefault: true, description: PrefixAnalyzerDescription);

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        INamedTypeSymbol symbol = (INamedTypeSymbol)context.Symbol;
        string? prefix = null;

        if (symbol.TypeKind == TypeKind.Struct && AnalyzerStatics.HasAttribute(symbol, AnalyzerStatics.UStructAttribute))
        {
            prefix = "F";
        }
        else if (symbol.TypeKind == TypeKind.Enum && AnalyzerStatics.HasAttribute(symbol, AnalyzerStatics.UEnumAttribute))
        {
            prefix = "E";
        }
        else if (symbol.TypeKind == TypeKind.Class)
        {
            if (!AnalyzerStatics.HasAttribute(symbol, AnalyzerStatics.UClassAttribute))
            {
                return;
            }

            if (AnalyzerStatics.InheritsFrom(symbol, AnalyzerStatics.AActor))
            {
                prefix = "A";
            }
            else if (AnalyzerStatics.InheritsFrom(symbol, AnalyzerStatics.UObject))
            {
                prefix = "U";
            }
        }
        else if (symbol.TypeKind == TypeKind.Interface && AnalyzerStatics.HasAttribute(symbol, AnalyzerStatics.UInterfaceAttribute))
        {
            prefix = "I";
        }

        if (prefix == null || symbol.Name.StartsWith(prefix))
        {
            return;
        }

        Dictionary<string, string> properties = new Dictionary<string, string>
        {
            { "Prefix", prefix }
        };

        Diagnostic diagnostic = Diagnostic.Create(PrefixRule, symbol.Locations[0], properties.ToImmutableDictionary(), symbol.TypeKind.ToString(), symbol.Name, prefix);
        context.ReportDiagnostic(diagnostic);
    }
}