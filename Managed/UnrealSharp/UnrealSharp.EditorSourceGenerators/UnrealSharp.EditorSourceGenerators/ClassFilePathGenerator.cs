using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.EditorSourceGenerators;

struct AssemblyClassInfo(string fullName, string filePath)
{
    public string FullName = fullName;
    public string FilePath = filePath;
}

[Generator]
public class ClassFilePathGenerator : IIncrementalGenerator
{
    private static string GetRelativePath(string filePath)
    {
        filePath = filePath.Replace("\\", "/");
        
        int index = filePath.IndexOf("/Script", StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            return filePath.Substring(index);
        }

        return filePath;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
            static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
            static (context, _) =>
            {
                return context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol classSymbol
                    ? (new[] { new AssemblyClassInfo(classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), context.Node.SyntaxTree.FilePath) })
                    : Array.Empty<AssemblyClassInfo>();
            })
            .SelectMany((results, _) => results)
            .Where(i => i.FilePath != null)
            .Collect()
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(syntaxProvider, (outputContext, info) =>
        {
            var (classes, compilation) = info;

            Dictionary<string, string> classDictionary = new Dictionary<string, string>();

            foreach (AssemblyClassInfo classInfo in classes)
            {
                string className = classInfo.FullName;
                string? relativeFilePath = GetRelativePath(classInfo.FilePath);

                if (!string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(relativeFilePath))
                {
                    classDictionary[className] = relativeFilePath;
                }
            }

            StringBuilder sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine("using System.Collections.Generic;");
            sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
            sourceBuilder.AppendLine($"namespace {compilation.AssemblyName};");

            sourceBuilder.AppendLine("public static class ClassFileMap");
            sourceBuilder.AppendLine("{");

            sourceBuilder.AppendLine("    [ModuleInitializer]");
            sourceBuilder.AppendLine("    public static void Initialize()");
            sourceBuilder.AppendLine("    {");

            foreach (KeyValuePair<string, string> kvp in classDictionary)
            {
                sourceBuilder.AppendLine($"        AddClassFile(\"{kvp.Key}\", \"{kvp.Value}\");");
            }

            sourceBuilder.AppendLine("    }");

            sourceBuilder.AppendLine("    public unsafe static void AddClassFile(string className, string filePath)");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        fixed (char* ptr1 = className)");
            sourceBuilder.AppendLine("        fixed (char* ptr2 = filePath)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            UnrealSharp.Interop.FCSTypeRegistryExporter.CallRegisterClassToFilePath(ptr1, ptr2);");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");

            sourceBuilder.AppendLine("}");

            outputContext.AddSource("ClassFileMap.generated.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        });
    }
}
