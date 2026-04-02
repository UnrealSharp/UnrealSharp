using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.GlueGenerator.Exceptions;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public record struct ParseResult
{
    public readonly UnrealType? Type;
    public readonly string SymbolName;
    public readonly Exception? ParseException;

    public ParseResult(UnrealType? type, string symbolName, Exception? exception) : this()
    {
        Type = type;
        SymbolName = symbolName;
        ParseException = exception;
    }

    public bool Equals(ParseResult? other)
    {
        if (other is null || Type is null || other.Value.Type is null)
        {
            return false;
        }

        return Type == other.Value.Type;
    }

    public override int GetHashCode()
    {
        if (Type is null)
        {
            return 0;
        }
        
        return Type.GetHashCode();
    }
}

[Generator(LanguageNames.CSharp)]
public sealed class UnrealTypeDiscoveryGenerator : IIncrementalGenerator
{
    private const string UnrealSharpStackTraceTitle = "UnrealSharp StackTrace";
    private const string UnrealSharpGeneratorCategory = "UnrealSharp Glue Generator";
    
    private static readonly DiagnosticDescriptor GenerateSourceErrorDescriptor = new("USG001",
        "UnrealSharp Generation Failed",
        "Failed to generate source for '{0}' due to {1}",
        UnrealSharpGeneratorCategory, DiagnosticSeverity.Error, true);
    
    private static readonly DiagnosticDescriptor StartStackTraceErrorDescriptor = new("USG002",
        UnrealSharpStackTraceTitle, "StackTrace:", UnrealSharpGeneratorCategory, DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor StackTraceErrorDescriptor = new("USG003",
        UnrealSharpStackTraceTitle, "{0}", UnrealSharpGeneratorCategory, DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor EndStackTraceErrorDescriptor = new("USG004",
        UnrealSharpStackTraceTitle, "End of StackTrace", UnrealSharpGeneratorCategory, DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        List<InspectorData> globalInspectors = InspectionDispatcher.GetInspectorsForScope("Global");

        foreach (InspectorData inspector in globalInspectors)
        {
            string attributeName = inspector.InspectAttribute.FullyQualifiedAttributeName;
            IncrementalValuesProvider<ParseResult> parsedUnrealTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
                attributeName, IsGenerationCandidate, ParseUnrealType);
            
            context.RegisterSourceOutput(parsedUnrealTypes, ProcessParsedUnrealType);
        }
    }

    private bool IsGenerationCandidate(SyntaxNode token, CancellationToken cancellationToken)
    {
        return true;
    }
    
    private static ParseResult ParseUnrealType(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        string symbolName = ctx.TargetSymbol.Name;
        UnrealType? newType = null;
        Exception? error = null;

        try
        {
            INamedTypeSymbol attributeClass = ctx.Attributes[0].AttributeClass!;
            InspectorData? inspector = InspectionDispatcher.GetInspector(attributeClass.Name);

            if (inspector == null)
            {
                throw new InvalidOperationException($"No inspector found for {attributeClass.Name}");
            }

            newType = inspector.ApplyInspection(null, ctx.TargetNode, ctx, ctx.TargetSymbol, ctx.Attributes);

            if (ctx.TargetNode is TypeDeclarationSyntax typeDeclarationSyntax)
            {
                ITypeSymbol typeSymbol = (ITypeSymbol) ctx.TargetSymbol;
                InspectionDispatcher.InspectMembers(newType, typeSymbol, typeDeclarationSyntax, ctx);
            }
        }
        catch (Exception exception)
        {
            error = exception;
        }
        
        return new ParseResult(newType, symbolName, error);
    }

    private void ProcessParsedUnrealType(SourceProductionContext sourceProductionContext, ParseResult parseResult)
    {
        if (parseResult.ParseException != null)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                GenerateSourceErrorDescriptor, 
                Location.None,
                parseResult.SymbolName, 
                parseResult.ParseException.Message);

            ReportException(diagnostic, parseResult.ParseException, sourceProductionContext);
            return;
        }

        EmitUnrealTypeSource(sourceProductionContext, parseResult.Type!);
    }

    private static void EmitUnrealTypeSource(SourceProductionContext sourceProductionContext, UnrealType unrealType)
    {
        try
        {
            GeneratorStringBuilder builder = new GeneratorStringBuilder();
            builder.BeginGeneratedSourceFile(unrealType);

            unrealType.ExportType(builder, sourceProductionContext);
            builder.GenerateTypeRegistration(unrealType);

            sourceProductionContext.AddSource($"{unrealType.SourceName}.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
        }
        catch (Exception exception)
        {
            Diagnostic diagnostic = Diagnostic.Create(GenerateSourceErrorDescriptor, Location.None, unrealType.FullName, exception.Message);
            ReportException(diagnostic, exception, sourceProductionContext);
        }
    }

    static void ReportException(Diagnostic diagnostic, Exception exception, SourceProductionContext sourceProductionContext)
    {
        sourceProductionContext.ReportDiagnostic(diagnostic);
        
        if (exception is not ParseReflectionException)
        {
            List<string> stackTraceLines = new List<string>();
            string? stackTrace = exception.StackTrace;

            if (stackTrace != null && stackTrace.Length > 0)
            {
                string[] lines = stackTrace.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
                stackTraceLines.AddRange(lines);
            }
            else
            {
                stackTraceLines.Add("No stack trace available.");
            }
            
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(StartStackTraceErrorDescriptor, Location.None));
        
            foreach (string line in stackTraceLines)
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(StackTraceErrorDescriptor, Location.None, line.Trim()));
            }
        
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(EndStackTraceErrorDescriptor, Location.None));
        }
    }
}