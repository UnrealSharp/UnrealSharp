using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnrealSharp.SourceGenerators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PrefixAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PrefixAnalyzer";
    private static readonly LocalizableString Title = "Type name prefix analyzer";
    private static readonly LocalizableString MessageFormat = "Type '{0}' should have prefix '{1}'";
    private static readonly LocalizableString Description = "Ensures types have appropriate prefixes.";
    private const string Category = "Naming";

    private static DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
        string prefix = null;

        if (namedTypeSymbol.TypeKind == TypeKind.Struct)
        {
            if (!HasAttribute(namedTypeSymbol, "UStructAttribute"))
            {
                return;
            }
            
            prefix = "F";
        }
        else if (namedTypeSymbol.TypeKind == TypeKind.Enum)
        {
            if (!HasAttribute(namedTypeSymbol, "UEnumAttribute"))
            {
                return;
            }
            
            prefix = "E";
        }
        else if (namedTypeSymbol.TypeKind == TypeKind.Class)
        {
            if (!HasAttribute(namedTypeSymbol, "UClassAttribute"))
            {
                return;
            }
            
            if (InheritsFrom(namedTypeSymbol, "AActor"))
            {
                prefix = "A";
            }
            else if (InheritsFrom(namedTypeSymbol, "UObject"))
            {
                prefix = "U";
            }
        }
        
        if (prefix != null && !namedTypeSymbol.Name.StartsWith(prefix))
        {
            var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, prefix);
            context.ReportDiagnostic(diagnostic);
        }
    }
    
    private static bool HasAttribute(INamedTypeSymbol symbol, string attributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.Name == attributeName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool InheritsFrom(INamedTypeSymbol symbol, string baseTypeName)
    {
        INamedTypeSymbol currentSymbol = symbol;

        while (currentSymbol != null)
        {
            if (currentSymbol.Name == baseTypeName)
            {
                return true;
            }
            currentSymbol = currentSymbol.BaseType;
        }

        return false;
    }
}