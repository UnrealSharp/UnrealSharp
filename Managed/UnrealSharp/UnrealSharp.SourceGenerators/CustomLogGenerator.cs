using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators;

[Generator]
public class CustomLogSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new CustomLogSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not CustomLogSyntaxReceiver receiver)
        {
            return;
        }
        
        foreach (var classDeclaration in receiver.ClassesWithCustomLog)
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            if (classSymbol == null)
            {
                continue;
            }
            
            foreach (var attributeList in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString() != "CustomLog")
                    {
                        continue;
                    }
                    
                    AttributeArgumentSyntax? firstArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    
                    string logVerbosity;
                    if (firstArgument != null)
                    {
                        logVerbosity = firstArgument.Expression.ToString();
                    }
                    else
                    {
                        logVerbosity = "ELogVerbosity.Display";
                    }
                    
                    string generatedCode = GenerateLoggerClass(classSymbol, classSymbol.Name, logVerbosity);
                    context.AddSource($"{classSymbol.Name}_CustomLog.generated.cs", SourceText.From(generatedCode, Encoding.UTF8));
                }
            }
        }
    }

    private string GenerateLoggerClass(INamedTypeSymbol classSymbol, string logFieldName, string logVerbosity)
    {
        string namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        string className = classSymbol.Name;
        StringBuilder builder = new StringBuilder();
        
        builder.AppendLine("using UnrealSharp.Logging;");

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

    private class CustomLogSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassesWithCustomLog { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration && classDeclaration.AttributeLists.Count > 0)
            {
                ClassesWithCustomLog.Add(classDeclaration);
            }
        }
    }
}