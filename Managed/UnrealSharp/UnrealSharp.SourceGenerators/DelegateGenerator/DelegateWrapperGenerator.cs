using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators.DelegateGenerator;

public class DelegateInheritanceSyntaxReceiver : ISyntaxReceiver
{
    public List<MemberDeclarationSyntax> Delegates { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is MemberDeclarationSyntax memberDeclarationSyntax
            && AnalyzerStatics.HasAttribute(memberDeclarationSyntax, "Binding"))
        {
            return;
        }
        
        if (syntaxNode is ClassDeclarationSyntax classDecl && classDecl.BaseList != null &&
            classDecl.BaseList.Types.Any(bt => bt.Type.ToString().Contains("MulticastDelegate") || bt.Type.ToString().Contains("Delegate")))
        {
            Delegates.Add(classDecl);
        }
        else if (syntaxNode is DelegateDeclarationSyntax delegateDecl)
        {
            if (AnalyzerStatics.HasAttribute(delegateDecl, "GeneratedType"))
            {
                return;
            }
            
            Delegates.Add(delegateDecl);
        }
    }
}

[Generator]
public class DelegateWrapperGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new DelegateInheritanceSyntaxReceiver());
    }

    public void  Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not DelegateInheritanceSyntaxReceiver receiver)
        {
            return;
        }
        
        foreach (var delegateClass in receiver.Delegates)
        {
            // Obtain the SemanticModel for the current syntax tree
            var model = context.Compilation.GetSemanticModel(delegateClass.SyntaxTree);

            // Get the symbol for the class declaration
            if (model.GetDeclaredSymbol(delegateClass) is not INamedTypeSymbol symbol)
            {
                continue;
            }
            
            if (symbol.IsGenericType || AnalyzerStatics.HasAttribute(symbol, "UnmanagedFunctionPointerAttribute"))
            {
                continue;
            }
            
            INamedTypeSymbol delegateSymbol = symbol;
            string delegateName = null;
            bool generateInvoker = true;
            
            if (delegateClass is ClassDeclarationSyntax classDeclaration)
            {
                delegateName = classDeclaration.Identifier.ValueText;
                delegateSymbol = (INamedTypeSymbol) symbol.BaseType.TypeArguments.FirstOrDefault();
                generateInvoker = !symbol.GetMembers().Any(x => x.Name == "Invoker");
            }
            else if (delegateClass is DelegateDeclarationSyntax delegateDeclaration)
            {
                delegateName = "U" + delegateDeclaration.Identifier.ValueText;
            }
            
            if (string.IsNullOrEmpty(delegateName) || delegateSymbol == null)
            {
                continue;
            }
            
            var typeSymbol = context.Compilation.GetSemanticModel(delegateClass.SyntaxTree).GetDeclaredSymbol(delegateClass);
            string namespaceName = typeSymbol?.ContainingNamespace.ToDisplayString() ?? "Global";
            
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using UnrealSharp;");
            stringBuilder.AppendLine("using UnrealSharp.Interop;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace {namespaceName};");
            stringBuilder.AppendLine();

            DelegateBuilder builder;
            
            string baseType;
            DelegateType delegateType;
            if (AnalyzerStatics.HasAttribute(delegateSymbol, AnalyzerStatics.USingleDelegateAttribute))
            {
                baseType = "Delegate";
                delegateType = DelegateType.Single;
            }
            else if (AnalyzerStatics.HasAttribute(delegateSymbol, AnalyzerStatics.UMultiDelegateAttribute))
            {
                baseType = "MulticastDelegate";
                delegateType = DelegateType.Multicast;
            }
            else
            {
                continue;
            }

            stringBuilder.AppendLine($"public partial class {delegateName} : {baseType}<{delegateSymbol}>");
            stringBuilder.AppendLine("{");
            
            if (delegateType == DelegateType.Multicast)
            {
                builder = new MulticastDelegateBuilder();
            }
            else
            {
                builder = new SingleDelegateBuilder();
            }
            
            builder.StartBuilding(stringBuilder, delegateSymbol, delegateName!, generateInvoker);
                    
            stringBuilder.AppendLine("}");
            stringBuilder.AppendLine();
            
            GenerateDelegateExtensionsClass(stringBuilder, delegateSymbol, delegateName!, delegateType);
            
            context.AddSource($"{namespaceName}.{delegateName}.generated.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
        }
    }
    
    public static void GenerateDelegateExtensionsClass(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string delegateName, DelegateType delegateType)
    {
        stringBuilder.AppendLine($"public static class {delegateName}Extensions");
        stringBuilder.AppendLine("{");
            
        var parametersList = delegateSymbol.DelegateInvokeMethod!.Parameters.ToList();
            
        string args = parametersList.Any()
            ? string.Join(", ", parametersList.Select(x => $"{(x.RefKind == RefKind.Ref ? "ref " : x.RefKind == RefKind.Out ? "out " : string.Empty)}{x.Type} {x.Name}"))
            : string.Empty;
            
        string parameters = parametersList.Any()
            ? string.Join(", ", parametersList.Select(x => $"{(x.RefKind == RefKind.Ref ? "ref " : x.RefKind == RefKind.Out ? "out " : string.Empty)}{x.Name}"))
            : string.Empty;
        
        string delegateTypeString = delegateType == DelegateType.Multicast ? "TMulticastDelegate" : "TDelegate";

        stringBuilder.AppendLine($"     public static void Invoke(this {delegateTypeString}<{delegateSymbol}> @delegate{(args.Any() ? $", {args}" : string.Empty)})");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         @delegate.InnerDelegate.Invoke({parameters});");
        stringBuilder.AppendLine("     }");
        stringBuilder.AppendLine("}");
    }
}