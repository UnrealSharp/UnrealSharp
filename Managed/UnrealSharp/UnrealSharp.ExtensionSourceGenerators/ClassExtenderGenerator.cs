using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.ExtensionSourceGenerators;

[Generator]
public class ClassExtenderGenerator : ISourceGenerator
{
    private readonly Dictionary<INamedTypeSymbol, ExtensionGenerator> _generators = new();

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassSyntaxReceiver());
    }

    private void RegisterGenerator(INamedTypeSymbol symbol, ExtensionGenerator generator)
    {
        _generators.Add(symbol, generator);
    }

    private ExtensionGenerator? GetGenerator(ITypeSymbol symbol)
    {
        foreach (var baseType in _generators)
        {
            if (IsA(symbol, baseType.Key))
            {
                return baseType.Value;
            }
        }

        return null;
    }
    
    void InitializeGenerators(GeneratorExecutionContext context)
    {
        if (_generators.Count > 0)
        {
            return;
        }
        
        RegisterGenerator(context.Compilation.GetTypeByMetadataName("UnrealSharp.Engine.AActor")!, new ActorExtensionGenerator());
        RegisterGenerator(context.Compilation.GetTypeByMetadataName("UnrealSharp.Engine.UActorComponent")!, new ActorComponentExtensionGenerator());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not ClassSyntaxReceiver receiver)
        {
            return;
        }
        
        InitializeGenerators(context);
        
        foreach (var classDeclaration in receiver.CandidateClasses)
        {
            var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            if (model.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
            {
                continue;
            }
            
            ExtensionGenerator? generator = GetGenerator(classSymbol);
            
            if (generator == null)
            {
                continue;
            }
                
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("#nullable disable");
            stringBuilder.AppendLine();
            
            stringBuilder.AppendLine("using UnrealSharp.Engine;");
            stringBuilder.AppendLine("using UnrealSharp.CoreUObject;");
            stringBuilder.AppendLine("using UnrealSharp;");
            stringBuilder.AppendLine();
        
            stringBuilder.AppendLine($"namespace {classSymbol.ContainingNamespace};");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"public partial class {classSymbol.Name}");
            stringBuilder.AppendLine("{");
            
            generator.Generate(ref stringBuilder, classSymbol);
                
            stringBuilder.AppendLine("}");
            context.AddSource($"{classSymbol.Name}.generated.extension.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
        }
    }
    
    private static bool IsA(ITypeSymbol classSymbol, ITypeSymbol otherSymbol)
    {
        var currentSymbol = classSymbol.BaseType;

        while (currentSymbol != null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentSymbol, otherSymbol))
            {
                return true;
            }

            currentSymbol = currentSymbol.BaseType;
        }

        return false;
    }

    private class ClassSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = [];

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax { BaseList: not null } classDeclaration)
            {
                CandidateClasses.Add(classDeclaration);
            }
        }
    }
}