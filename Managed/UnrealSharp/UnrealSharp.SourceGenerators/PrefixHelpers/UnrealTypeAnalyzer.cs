using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnrealSharp.SourceGenerators.PrefixHelpers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnrealTypeAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Naming";
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(PrefixRule, PropertyRule, StructRule, ClassRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeStructFields, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeClassFields, SymbolKind.Field);
    }

    public const string StructAnalyzerId = "StructFieldAnalyzer";
    public const string ClassAnalyzerId = "ClassFieldAnalyzer";
    private static readonly LocalizableString StructAnalyzerTitle = "UnrealSharp Struct Field Analyzer";
    private static readonly LocalizableString ClassAnalyzerTitle = "UnrealSharp Class Field Analyzer";
    private static readonly LocalizableString StructAnalyzerMessageFormat = "{0} is a UProperty and a property, which is not allowed in structs. UProperties in structs must be fields.";
    private static readonly LocalizableString ClassAnalyzerMessageFormat = "{0} is a UProperty and a field, which is not allowed in classes. UProperties in classes must be properties.";
    private static readonly LocalizableString StructAnalyzerDescription = "Ensures UProperties in structs are fields.";
    private static readonly LocalizableString ClassAnalyzerDescription = "Ensures UProperties in classes are properties.";

    public static readonly DiagnosticDescriptor StructRule = new(StructAnalyzerId, StructAnalyzerTitle, StructAnalyzerMessageFormat, "Category", DiagnosticSeverity.Error, isEnabledByDefault: true, description: StructAnalyzerDescription);
    public static readonly DiagnosticDescriptor ClassRule = new(ClassAnalyzerId, ClassAnalyzerTitle, ClassAnalyzerMessageFormat, "Category", DiagnosticSeverity.Error, isEnabledByDefault: true, description: ClassAnalyzerDescription);

    public static void AnalyzeFields(SymbolAnalysisContext context, TypeKind typeKind, string requiredAttribute, DiagnosticDescriptor rule)
    {
        ISymbol symbol = context.Symbol;
        INamedTypeSymbol type = symbol.ContainingType;

        if (type.TypeKind != typeKind && !PrefixStatics.HasAttribute(type, requiredAttribute))
        {
            return;
        }

        if (!PrefixStatics.HasAttribute(symbol, PrefixStatics.UPropertyAttribute))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(rule, symbol.Locations[0], symbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    public static void AnalyzeStructFields(SymbolAnalysisContext context)
    {
        if (context.Symbol is IPropertySymbol)
        {
            AnalyzeFields(context, TypeKind.Struct, PrefixStatics.UStructAttribute, StructRule);
        }
    }

    public static void AnalyzeClassFields(SymbolAnalysisContext context)
    {
        if (context.Symbol is IFieldSymbol)
        {
            AnalyzeFields(context, TypeKind.Class, PrefixStatics.UClassAttribute, ClassRule);
        }
    }

    public const string PropertyAnalyzerId = "PropertyAnalyzer";
    private static readonly LocalizableString PropertyAnalyzerTitle = "UnrealSharp Property Analyzer";
    private static readonly LocalizableString PropertyAnalyzerMessageFormat = "{0} '{1}' is exposed to Unreal Engine, but it's type does not have the '{2}' attribute";
    private static readonly LocalizableString PropertyAnalyzerDescription = "Ensures properties have appropriate prefixes.";
    
    public static readonly DiagnosticDescriptor PropertyRule = new(PropertyAnalyzerId, PropertyAnalyzerTitle, PropertyAnalyzerMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: PropertyAnalyzerDescription);

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        ISymbol symbol = context.Symbol;
        
        ITypeSymbol type;
        ITypeSymbol containingType;
        
        // Need to handle fields and properties separately. Since we use properties in classes, but fields in structs
        if (symbol is IPropertySymbol propertySymbol)
        {
            type = propertySymbol.Type;
            containingType = propertySymbol.ContainingType;
        }
        else
        {
            IFieldSymbol fieldSymbol = (IFieldSymbol) symbol;
            type = fieldSymbol.Type;
            containingType = fieldSymbol.ContainingType;
        }
        
        if (!type.IsValueType && !type.IsReferenceType)
        {
            return;
        }
        
        if (!PrefixStatics.HasAttribute(containingType, PrefixStatics.UClassAttribute) 
            && !PrefixStatics.HasAttribute(containingType, PrefixStatics.UStructAttribute))
        {
            return;
        }

        if (!PrefixStatics.HasAttribute(symbol, PrefixStatics.UPropertyAttribute))
        {
            return;
        }
        
        string attribute = null;
        
        if (type.TypeKind == TypeKind.Class && !PrefixStatics.HasAttribute(type, PrefixStatics.UClassAttribute))
        {
            attribute = "UClass";
        }
        
        if (type.TypeKind == TypeKind.Struct && !PrefixStatics.HasAttribute(type, PrefixStatics.UStructAttribute))
        {
            attribute = "UStruct";
        }
        
        if (type.TypeKind == TypeKind.Enum && !PrefixStatics.HasAttribute(type, PrefixStatics.UEnumAttribute))
        {
            attribute = "UEnum";
        }
        
        if (type.TypeKind == TypeKind.Interface && !PrefixStatics.HasAttribute(type, PrefixStatics.UInterfaceAttribute))
        {
            attribute = "UInterface";
        }
        
        if (attribute == null)
        {
            return;
        }
        
        var diagnostic = Diagnostic.Create(PropertyRule, symbol.Locations[0], "Property", symbol.Name, attribute);
        context.ReportDiagnostic(diagnostic);
    }


    public const string PrefixAnalyzerId = "PrefixAnalyzer";
    private static readonly LocalizableString PrefixAnalyzerTitle = "UnrealSharp Prefix Analyzer";
    private static readonly LocalizableString PrefixAnalyzerMessageFormat = "{0} '{1}' is exposed to Unreal Engine and should have prefix '{2}'";
    private static readonly LocalizableString PrefixAnalyzerDescription = "Ensures types have appropriate prefixes.";
    public static readonly DiagnosticDescriptor PrefixRule = new(PrefixAnalyzerId, PrefixAnalyzerTitle, PrefixAnalyzerMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: PrefixAnalyzerDescription);

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        INamedTypeSymbol symbol = (INamedTypeSymbol) context.Symbol;
        string prefix = null;

        // These types are generated by the script generator, and already have the correct prefix
        if (PrefixStatics.HasAttribute(symbol, PrefixStatics.GeneratedTypeAttribute))
        {
            return;
        }
        
        if (symbol.TypeKind == TypeKind.Struct && PrefixStatics.HasAttribute(symbol, PrefixStatics.UStructAttribute))
        {
            prefix = "F";
        }
        else if (symbol.TypeKind == TypeKind.Enum && PrefixStatics.HasAttribute(symbol, PrefixStatics.UEnumAttribute))
        {
            prefix = "E";
        }
        else if (symbol.TypeKind == TypeKind.Class)
        {
            if (!PrefixStatics.HasAttribute(symbol, PrefixStatics.UClassAttribute))
            {
                return;
            }
            
            if (PrefixStatics.InheritsFrom(symbol, PrefixStatics.AActor))
            {
                prefix = "A";
            }
            else if (PrefixStatics.InheritsFrom(symbol, PrefixStatics.UObject))
            {
                prefix = "U";
            }
        }
        else if (symbol.TypeKind == TypeKind.Interface && PrefixStatics.HasAttribute(symbol, PrefixStatics.UInterfaceAttribute))
        {
            prefix = "I";
        }

        if (prefix == null || symbol.Name.StartsWith(prefix))
        {
            return;
        }
        
        var diagnostic = Diagnostic.Create(PrefixRule, symbol.Locations[0], symbol.TypeKind.ToString(), symbol.Name, prefix);
        context.ReportDiagnostic(diagnostic);
    }
}