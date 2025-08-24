using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.ExtensionSourceGenerators;

[Generator]
public class ClassExtenderGenerator : IIncrementalGenerator
{    
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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var actorExtensionGenerator = new ActorExtensionGenerator();
        var actorComponentExtensionGenerator = new ActorComponentExtensionGenerator();

        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider<(INamedTypeSymbol Symbol, ExtensionGenerator Generator)>(
            static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax { BaseList: not null },
            (context, _) =>
            {
                if (context.SemanticModel.GetTypeInfo(context.Node).Type is not INamedTypeSymbol typeSymbol)
                {
                    return (null!, null!);
                }

                if (IsA(typeSymbol, context.SemanticModel.Compilation.GetTypeByMetadataName("UnrealSharp.Engine.AActor")!))
                {
                    return (typeSymbol, actorExtensionGenerator);
                }
                else if (IsA(typeSymbol, context.SemanticModel.Compilation.GetTypeByMetadataName("UnrealSharp.Engine.UActorComponent")!))
                {
                    return (typeSymbol, actorComponentExtensionGenerator);
                }
                else
                {
                    return (null!, null!);
                }
            })
            .Where(classDecl => classDecl.Symbol != null);

        context.RegisterSourceOutput(syntaxProvider, (outputContext, classDecl) =>
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("#nullable disable");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("using UnrealSharp.Engine;");
            stringBuilder.AppendLine("using UnrealSharp.CoreUObject;");
            stringBuilder.AppendLine("using UnrealSharp;");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"namespace {classDecl.Symbol.ContainingNamespace};");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"public partial class {classDecl.Symbol.Name}");
            stringBuilder.AppendLine("{");

            classDecl.Generator.Generate(ref stringBuilder, classDecl.Symbol);

            stringBuilder.AppendLine("}");
            outputContext.AddSource($"{classDecl.Symbol.Name}.generated.extension.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
        });
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