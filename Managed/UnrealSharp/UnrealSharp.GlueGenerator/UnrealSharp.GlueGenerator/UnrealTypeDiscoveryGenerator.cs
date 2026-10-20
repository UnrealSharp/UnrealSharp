using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.GlueGenerator.Exceptions;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public readonly record struct ParseResult
{
    public readonly UnrealType? Type;
    public readonly string SymbolName;
    public readonly string? ErrorMessage;
    public readonly string? ErrorStackTrace;
    public readonly bool IsParseReflectionException;
    public readonly Location Location;

    public ParseResult(UnrealType? type, string symbolName, Location location, Exception? exception)
    {
        Type = type;
        SymbolName = symbolName;
        Location = location;
        ErrorMessage = exception?.Message;
        ErrorStackTrace = exception?.StackTrace;
        IsParseReflectionException = exception is ParseReflectionException;
    }

    public bool HasError => ErrorMessage != null;

    public bool Equals(ParseResult other)
    {
        return Equals(Type, other.Type)
               && SymbolName == other.SymbolName
               && ErrorMessage == other.ErrorMessage;
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Type?.GetHashCode() ?? 0);
        hash.Add(SymbolName);
        hash.Add(ErrorMessage ?? string.Empty);
        return hash.ToHashCode();
    }
}

[Generator(LanguageNames.CSharp)]
public sealed class UnrealTypeDiscoveryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        List<InspectorData> globalInspectors = InspectionDispatcher.GetInspectorsForScope("Global");
        List<IncrementalValuesProvider<ParseResult>> allProviders =
            new List<IncrementalValuesProvider<ParseResult>>(globalInspectors.Count);

        foreach (InspectorData inspector in globalInspectors)
        {
            string attributeName = inspector.InspectAttribute.FullyQualifiedAttributeName;

            IncrementalValuesProvider<ParseResult> parsedUnrealTypes =
                context.SyntaxProvider.ForAttributeWithMetadataName(attributeName, IsGenerationCandidate,
                    ParseUnrealType);

            context.RegisterSourceOutput(parsedUnrealTypes, ProcessParsedUnrealType);
            allProviders.Add(parsedUnrealTypes);
        }

        if (allProviders.Count == 0)
        {
            return;
        }

        IncrementalValueProvider<ImmutableArray<ParseResult>> merged = allProviders[0].Collect();

        for (int i = 1; i < allProviders.Count; i++)
        {
            merged = merged
                .Combine(allProviders[i].Collect())
                .Select(static (pair, _) => pair.Left.AddRange(pair.Right));
        }

        context.RegisterSourceOutput(merged, ValidateWholeCompilation);
    }

    private static bool IsGenerationCandidate(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is TypeDeclarationSyntax
            or EnumDeclarationSyntax
            or DelegateDeclarationSyntax
            or PropertyDeclarationSyntax
            or MethodDeclarationSyntax
            or FieldDeclarationSyntax;
    }

    private static ParseResult ParseUnrealType(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        string symbolName = ctx.TargetSymbol.Name;
        Location location = ctx.TargetNode.GetLocation();
        UnrealType? newType = null;
        Exception? error = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            INamedTypeSymbol attributeClass = ctx.Attributes[0].AttributeClass!;
            InspectorData? inspector = InspectionDispatcher.GetInspector(attributeClass.Name);

            if (inspector == null)
            {
                throw new InvalidOperationException($"No inspector found for {attributeClass.Name}");
            }

            newType = inspector.ApplyInspection(null, ctx.TargetNode, ctx, ctx.TargetSymbol, ctx.Attributes);

            if (ctx.TargetNode is TypeDeclarationSyntax typeDeclarationSyntax)
            {
                ITypeSymbol typeSymbol = (ITypeSymbol)ctx.TargetSymbol;
                InspectionDispatcher.InspectMembers(newType, typeSymbol, typeDeclarationSyntax, ctx);
            }

            newType.CollectDependencies();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            error = exception;
        }

        return new ParseResult(newType, symbolName, location, error);
    }

    private void ProcessParsedUnrealType(SourceProductionContext sourceProductionContext, ParseResult parseResult)
    {
        if (parseResult.HasError)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                Diagnostics.GenerationFailed,
                parseResult.Location,
                parseResult.SymbolName,
                parseResult.ErrorMessage);

            ReportException(diagnostic, parseResult, sourceProductionContext);
            return;
        }

        if (parseResult.Type == null)
        {
            return;
        }

        EmitUnrealTypeSource(sourceProductionContext, parseResult.Type);
    }

    private static void ValidateWholeCompilation(SourceProductionContext context, ImmutableArray<ParseResult> results)
    {
        List<UnrealType> types = new List<UnrealType>(results.Length);

        foreach (ParseResult result in results)
        {
            if (result.Type != null && !result.HasError)
            {
                types.Add(result.Type);
            }
        }

        Diagnostics.ValidateEngineNameUniqueness(types, context);
    }

    private static void EmitUnrealTypeSource(SourceProductionContext sourceProductionContext, UnrealType unrealType)
    {
        try
        {
            GeneratorStringBuilder builder = new GeneratorStringBuilder();
            builder.BeginGeneratedSourceFile(unrealType);

            unrealType.ExportType(builder, sourceProductionContext);

            string hintName = MakeHintName(unrealType.FieldName.FullName);
            sourceProductionContext.AddSource(hintName, SourceText.From(builder.ToString(), Encoding.UTF8));
        }
        catch (Exception exception)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                Diagnostics.GenerationFailed,
                Location.None,
                unrealType.FieldName.SourceName,
                exception.Message);

            ReportException(diagnostic,
                new ParseResult(null, unrealType.FieldName.SourceName, Location.None, exception),
                sourceProductionContext);
        }
    }

    private static string MakeHintName(string fullName)
    {
        StringBuilder builder = new StringBuilder(fullName.Length + 5);

        foreach (char character in fullName)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        builder.Append(".g.cs");
        return builder.ToString();
    }

    private static void ReportException(Diagnostic diagnostic, ParseResult parseResult,
        SourceProductionContext sourceProductionContext)
    {
        sourceProductionContext.ReportDiagnostic(diagnostic);

        if (parseResult.IsParseReflectionException)
        {
            return;
        }

        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.StackTraceStart, Location.None));

        string stackTrace = parseResult.ErrorStackTrace ?? string.Empty;

        if (stackTrace.Length == 0)
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.StackTraceLine, Location.None,
                "No stack trace available."));
        }
        else
        {
            foreach (string line in stackTrace.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.StackTraceLine, Location.None,
                    line.Trim()));
            }
        }

        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.StackTraceEnd, Location.None));
    }
}