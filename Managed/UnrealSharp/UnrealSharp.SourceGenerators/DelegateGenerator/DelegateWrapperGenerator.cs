using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators;

public class DelegateInheritanceSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax { BaseList: not null } classDecl &&
            classDecl.BaseList.Types.Any(bt => bt.Type.ToString().Contains("MulticastDelegate") || bt.Type.ToString().Contains("Delegate")))
        {
            CandidateClasses.Add(classDecl);
        }
    }
}

[Generator]
public class DelegateWrapperGenerator : ISourceGenerator
{
    private const string MultiCastDelegateName = "MulticastDelegate";
    private const string DelegateName = "Delegate";
    
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new DelegateInheritanceSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not DelegateInheritanceSyntaxReceiver receiver)
        {
            return;
        }
        
        foreach (var classDecl in receiver.CandidateClasses)
        {
            // Obtain the SemanticModel for the current syntax tree
            var model = context.Compilation.GetSemanticModel(classDecl.SyntaxTree);

            // Get the symbol for the class declaration

            if (model.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
            {
                continue;
            }
            
            // Check if the class inherits from MulticastDelegate
            string baseTypeName = classSymbol.BaseType.Name;

            if (baseTypeName is not (MultiCastDelegateName or DelegateName))
            {
                continue;
            }
            
            if (classSymbol.BaseType is not INamedTypeSymbol multicastDelegateSymbol)
            {
                continue;
            }

            // Extract the generic type argument
            var genericTypeArgument = multicastDelegateSymbol.TypeArguments.FirstOrDefault();
            
            if (genericTypeArgument == null)
            {
                continue;
            }
            
            var typeSymbol = context.Compilation.GetSemanticModel(classDecl.SyntaxTree).GetDeclaredSymbol(classDecl);
            string namespaceName = typeSymbol?.ContainingNamespace.ToDisplayString() ?? "Global";
            string className = $"{classDecl.Identifier.ValueText}";
            
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using UnrealSharp;");
            stringBuilder.AppendLine("using UnrealSharp.Interop;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace {namespaceName};");
            stringBuilder.AppendLine();
                
            stringBuilder.AppendLine($"public partial class {className}");
            stringBuilder.AppendLine("{");
            
            INamedTypeSymbol delegateSymbol = (INamedTypeSymbol) genericTypeArgument;

            DelegateBuilder builder;
            DelegateType delegateType = classSymbol.BaseType.Name == "MulticastDelegate" ? DelegateType.Multicast : DelegateType.Single;
            
            if (delegateType == DelegateType.Multicast)
            {
                builder = new MulticastDelegateBuilder();
            }
            else
            {
                builder = new SingleDelegateBuilder();
            }
            
            builder.StartBuilding(stringBuilder, delegateSymbol, classSymbol);
                    
            stringBuilder.AppendLine("}");
            context.AddSource($"{namespaceName}.{className}.generated.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
        }
    }
}