using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public static class Diagnostics
{
    private const string Category = "UnrealSharp Glue Generator";

    public static readonly DiagnosticDescriptor GenerationFailed = new(
        "USG001",
        "UnrealSharp generation failed",
        "Failed to generate source for '{0}'. Reason: {1}.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StackTraceStart = new(
        "USG002", "UnrealSharp stack trace", "Stack trace:", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StackTraceLine = new(
        "USG003", "UnrealSharp stack trace", "{0}", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StackTraceEnd = new(
        "USG004", "UnrealSharp stack trace", "End of stack trace", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateEngineName = new(
        "USG006",
        "Duplicate engine name",
        "'{0}' and '{1}' both map to engine name '{2}' in package '{3}'. Unreal doesn't allow two fields with the same name in a package. Rename one or set the engine name with [GeneratedType].",
        Category, DiagnosticSeverity.Error, true);

    public static void ValidateEngineNameUniqueness(IEnumerable<UnrealType> types, SourceProductionContext context)
    {
        Dictionary<string, FieldName> seen = new Dictionary<string, FieldName>();

        foreach (UnrealType type in types)
        {
            FieldName name = type.FieldName;

            if (!name.IsTypeReference)
            {
                continue;
            }

            string package = GetPackageKey(name.Namespace);
            string key = package + "/" + name.EngineName;

            if (seen.TryGetValue(key, out FieldName existing))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DuplicateEngineName, Location.None,
                    existing.FullName, name.FullName, name.EngineName, package));

                continue;
            }

            seen.Add(key, name);
        }
    }

    private static string GetPackageKey(string @namespace)
    {
        if (string.IsNullOrEmpty(@namespace))
        {
            return string.Empty;
        }

        int firstDot = @namespace.IndexOf('.');
        return firstDot < 0 ? @namespace : @namespace.Substring(0, firstDot);
    }
}