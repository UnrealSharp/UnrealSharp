using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators;

[Generator]
public class CustomLogSourceGenerator : IIncrementalGenerator
{
    private readonly struct ClassLogInfo
    {
        public readonly INamedTypeSymbol ClassSymbol;
        public readonly string LogVerbosity;
        public ClassLogInfo(INamedTypeSymbol classSymbol, string logVerbosity)
        {
            ClassSymbol = classSymbol;
            LogVerbosity = logVerbosity;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classLogInfos = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                static (syntaxContext, _) => GetClassLogInfos(syntaxContext))
            .SelectMany(static (infos, _) => infos)
            .Where(static info => info.ClassSymbol is not null);

        context.RegisterSourceOutput(classLogInfos, static (spc, info) =>
        {
            string source = GenerateLoggerClass(info.ClassSymbol, info.ClassSymbol.Name, info.LogVerbosity);
            spc.AddSource($"{info.ClassSymbol.Name}_CustomLog.generated.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static IEnumerable<ClassLogInfo> GetClassLogInfos(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return Array.Empty<ClassLogInfo>();
        }

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
        {
            return Array.Empty<ClassLogInfo>();
        }

        List<ClassLogInfo> list = new();

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName is not ("CustomLog" or "CustomLogAttribute"))
                {
                    continue;
                }

                var firstArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                string logVerbosity = firstArgument != null ? firstArgument.Expression.ToString() : "ELogVerbosity.Display";
                list.Add(new ClassLogInfo(classSymbol, logVerbosity));
            }
        }

        return list;
    }

    private static string GenerateLoggerClass(INamedTypeSymbol classSymbol, string logFieldName, string logVerbosity)
    {
        string namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        string className = classSymbol.Name;
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("using UnrealSharp.Log;");

        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine($"namespace {namespaceName};");
        }

        builder.AppendLine($"public partial class {className}");
        builder.AppendLine("{");
        builder.AppendLine($"    public static void Log(string message) => UnrealLogger.Log(\"{logFieldName}\", message, {logVerbosity});");
        builder.AppendLine($"    public static void LogWarning(string message) => UnrealLogger.LogWarning(\"{logFieldName}\", message);");
        builder.AppendLine($"    public static void LogError(string message) => UnrealLogger.LogError(\"{logFieldName}\", message);");
        builder.AppendLine($"    public static void LogFatal(string message) => UnrealLogger.LogFatal(\"{logFieldName}\", message);");
        builder.AppendLine("}");

        return builder.ToString();
    }
}