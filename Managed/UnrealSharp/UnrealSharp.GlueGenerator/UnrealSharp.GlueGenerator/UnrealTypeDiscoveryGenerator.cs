using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class UnrealTypeDiscoveryGenerator : IIncrementalGenerator
{
    private record struct ParseResult(UnrealType? Type, string SymbolName, Exception? Error = null);
    
    private const string UnrealSharpStackTraceTitle = "UnrealSharp StackTrace";
    private const string UnrealSharpGeneratorCategory = "UnrealSharp Glue Generator";
    
    private static readonly DiagnosticDescriptor GenerateSourceErrorDescriptor = new("USG001",
        "UnrealSharp Generation Failed",
        "Failed to generate source for '{0}'. Exception: {1}. See stack trace below for more details.",
        UnrealSharpGeneratorCategory, DiagnosticSeverity.Error, true);
    
    private static readonly DiagnosticDescriptor StartStackTraceErrorDescriptor = new("USG002",
        UnrealSharpStackTraceTitle, "StackTrace:", UnrealSharpGeneratorCategory, DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor StackTraceErrorDescriptor = new("USG003",
        UnrealSharpStackTraceTitle, "{0}", UnrealSharpGeneratorCategory, DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor EndStackTraceErrorDescriptor = new("USG004",
        UnrealSharpStackTraceTitle, "End of StackTrace", UnrealSharpGeneratorCategory, DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        List<InspectorData> inspectors = InspectorManager.GetScopedInspectorData("Global");

        foreach (InspectorData globalType in inspectors)
        {
            IncrementalValuesProvider<ParseResult> discoveryResults = context.SyntaxProvider.ForAttributeWithMetadataName(
                    globalType.InspectAttribute.FullyQualifiedAttributeName, CanGenerateType, ParseUnrealType);

            context.RegisterSourceOutput(discoveryResults, RegisterType);
        }
    }

    private bool CanGenerateType(SyntaxNode token, CancellationToken cancellationToken)
    {
        return true;
    }

    private void RegisterType(SourceProductionContext spc, ParseResult result)
    {
        if (result.Error != null)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                GenerateSourceErrorDescriptor, 
                Location.None,
                result.SymbolName, 
                result.Error.Message);

            ReportException(diagnostic, result.Error, spc);
            return;
        }

        EmitUnrealType(spc, result.Type!);
    }

    private static ParseResult ParseUnrealType(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        string symbolName = ctx.TargetSymbol.Name;

        try
        {
            INamedTypeSymbol? attributeClass = ctx.Attributes[0].AttributeClass;

            if (attributeClass == null)
            {
                throw new InvalidOperationException("Attribute class is null");
            }

            InspectorData? inspector = InspectorManager.GetInspectorData(attributeClass.Name);

            if (inspector == null)
            {
                throw new InvalidOperationException($"No inspector found for {attributeClass.Name}");
            }

            if (inspector.InspectAttributeDelegate == null)
            {
                throw new InvalidOperationException($"No inspector delegate found for {attributeClass.Name}");
            }

            UnrealType? type = inspector.InspectAttributeDelegate(null, ctx.TargetNode, ctx, ctx.TargetSymbol, ctx.Attributes);
            return new ParseResult(type, symbolName);
        }
        catch (Exception exception)
        {
            return new ParseResult(null, symbolName, exception);
        }
    }

    private static void EmitUnrealType(SourceProductionContext spc, UnrealType unrealType)
    {
        try
        {
            GeneratorStringBuilder builder = new GeneratorStringBuilder();
            builder.BeginGeneratedSourceFile(unrealType);

            unrealType.ExportType(builder, spc);
            builder.GenerateTypeRegistration(unrealType);

            spc.AddSource($"{unrealType.SourceName}.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
        }
        catch (Exception exception)
        {
            Diagnostic diagnostic = Diagnostic.Create(GenerateSourceErrorDescriptor, Location.None, unrealType.FullName, exception.Message);
            ReportException(diagnostic, exception, spc);
        }
    }

    static void ReportException(Diagnostic diagnostic, Exception ex, SourceProductionContext spc)
    {
        spc.ReportDiagnostic(diagnostic);
        
        string? stackTrace = ex.StackTrace;
        List<string> stackTraceLines = new List<string>();

        if (stackTrace != null && stackTrace.Length > 0)
        {
            string[] lines = stackTrace.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            stackTraceLines.AddRange(lines);
        }
        else
        {
            stackTraceLines.Add("No stack trace available.");
        }

        spc.ReportDiagnostic(Diagnostic.Create(StartStackTraceErrorDescriptor, Location.None));
        
        foreach (string line in stackTraceLines)
        {
            spc.ReportDiagnostic(Diagnostic.Create(StackTraceErrorDescriptor, Location.None, line.Trim()));
        }
        
        spc.ReportDiagnostic(Diagnostic.Create(EndStackTraceErrorDescriptor, Location.None));
    }
}