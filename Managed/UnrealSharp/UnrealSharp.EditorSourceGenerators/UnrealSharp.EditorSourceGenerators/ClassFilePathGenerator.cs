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
public class ClassFilePathGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassCollector());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not ClassCollector receiver)
        {
            return;
        }
        
        Dictionary<string, string> classDictionary = new Dictionary<string, string>();

        foreach (AssemblyClassInfo classInfo in receiver.Classes)
        {
            string className = classInfo.FullName;
            string relativeFilePath = GetRelativePath(classInfo.FilePath);

            if (!string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(relativeFilePath))
            {
                classDictionary[className] = relativeFilePath;
            }
        }
        
        StringBuilder sourceBuilder = new StringBuilder();
        
        sourceBuilder.AppendLine("using System.Collections.Generic;");
        sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
        sourceBuilder.AppendLine($"namespace {context.Compilation.AssemblyName};");
        
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

        context.AddSource("ClassFileMap.generated.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

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
}

class ClassCollector : ISyntaxReceiver
{
    public List<AssemblyClassInfo> Classes { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax classDecl)
        {
            return;
        }
        
        string className = GetFullClassName(classDecl);
        className = className.Substring(1);
        
        string filePath = GetRelativePath(classDecl.SyntaxTree.FilePath);
        Classes.Add(new AssemblyClassInfo(className, filePath));
    }
    
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

    private static string GetFullClassName(ClassDeclarationSyntax classDecl)
    {
        string className = classDecl.Identifier.Text;
        string namespaceName = "";

        SyntaxNode? current = classDecl.Parent;
        while (current is not null)
        {
            if (current is NamespaceDeclarationSyntax namespaceDecl)
            {
                namespaceName = namespaceDecl.Name.ToString();
                break;
            }
            current = current.Parent;
        }

        return string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}";
    }
}