using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators.DelegateGenerator;

[Generator]
public class DelegateWrapperGenerator : IIncrementalGenerator
{
    private sealed class DelegateGenerationInfo
    {
        public string NamespaceName { get; }
        public string DelegateName { get; }
        public INamedTypeSymbol DelegateSymbol { get; }
        public bool GenerateInvoker { get; }
        public DelegateType DelegateType { get; }
        public string BaseTypeName { get; }
        public bool NullableAwareable { get; }
        public DelegateGenerationInfo(string namespaceName, string delegateName, INamedTypeSymbol delegateSymbol, bool generateInvoker, DelegateType delegateType, string baseTypeName, bool nullableAwareable)
        {
            NamespaceName = namespaceName;
            DelegateName = delegateName;
            DelegateSymbol = delegateSymbol;
            GenerateInvoker = generateInvoker;
            DelegateType = delegateType;
            BaseTypeName = baseTypeName;
            NullableAwareable = nullableAwareable;

        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax { BaseList: not null } || syntaxNode is DelegateDeclarationSyntax,
                static (syntaxContext, _) => GetInfoOrNull(syntaxContext))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        context.RegisterSourceOutput(candidates, static (spc, info) => Generate(spc, info));
    }

    private static DelegateGenerationInfo? GetInfoOrNull(GeneratorSyntaxContext context)
    {
        // Exclude members with [Binding]
        if (context.Node is MemberDeclarationSyntax m && AnalyzerStatics.HasAttribute(m, "Binding"))
        {
            return null;
        }

        INamedTypeSymbol? symbol;
        INamedTypeSymbol? delegateSymbol;
        string delegateName;
        bool generateInvoker = true;
        DelegateType delegateType;
        string baseTypeName;

        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            // Must derive from *Delegate
            if (classDecl.BaseList == null || !classDecl.BaseList.Types.Any(bt => bt.Type.ToString().Contains("MulticastDelegate") || bt.Type.ToString().Contains("Delegate")))
            {
                return null;
            }

            symbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (symbol == null)
            {
                return null;
            }

            if (symbol.IsGenericType || AnalyzerStatics.HasAttribute(symbol, "UnmanagedFunctionPointerAttribute"))
            {
                return null;
            }

            delegateName = classDecl.Identifier.ValueText;
            delegateSymbol = symbol.BaseType?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
            if (delegateSymbol == null)
            {
                return null;
            }

            generateInvoker = !symbol.GetMembers().Any(x => x.Name == "Invoker");
        }
        else if (context.Node is DelegateDeclarationSyntax delegateDecl)
        {
            if (AnalyzerStatics.HasAttribute(delegateDecl, "GeneratedType"))
            {
                return null;
            }

            symbol = context.SemanticModel.GetDeclaredSymbol(delegateDecl) as INamedTypeSymbol;
            if (symbol == null)
            {
                return null;
            }

            if (symbol.IsGenericType || AnalyzerStatics.HasAttribute(symbol, "UnmanagedFunctionPointerAttribute"))
            {
                return null;
            }

            delegateName = "U" + delegateDecl.Identifier.ValueText;
            delegateSymbol = symbol; // Underlying delegate is the symbol itself
        }
        else
        {
            return null;
        }

        if (AnalyzerStatics.HasAttribute(delegateSymbol, AnalyzerStatics.USingleDelegateAttribute))
        {
            baseTypeName = "Delegate";
            delegateType = DelegateType.Single;
        }
        else if (AnalyzerStatics.HasAttribute(delegateSymbol, AnalyzerStatics.UMultiDelegateAttribute))
        {
            baseTypeName = "MulticastDelegate";
            delegateType = DelegateType.Multicast;
        }
        else
        {
            return null; // Not a recognized Unreal delegate wrapper
        }

        string namespaceName = symbol.ContainingNamespace?.ToDisplayString() ?? "Global";

        return new DelegateGenerationInfo(namespaceName, delegateName, delegateSymbol, generateInvoker, delegateType, baseTypeName,
            context.SemanticModel.GetNullableContext(context.Node.Span.Start).HasFlag(NullableContext.AnnotationsEnabled));
    }

    private static void Generate(SourceProductionContext context, DelegateGenerationInfo info)
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (info.NullableAwareable)
        {
            stringBuilder.AppendLine("#nullable enable");
        }
        else
        {
            stringBuilder.AppendLine("#nullable disable");
        }
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("using UnrealSharp;");
        stringBuilder.AppendLine("using UnrealSharp.Interop;");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"namespace {info.NamespaceName};");
        stringBuilder.AppendLine();

        DelegateBuilder builder = info.DelegateType == DelegateType.Multicast
            ? new MulticastDelegateBuilder()
            : new SingleDelegateBuilder();

        stringBuilder.AppendLine($"public partial class {info.DelegateName} : {info.BaseTypeName}<{info.DelegateSymbol}>");
        stringBuilder.AppendLine("{");
        builder.StartBuilding(stringBuilder, info.DelegateSymbol, info.DelegateName, info.GenerateInvoker);
        stringBuilder.AppendLine("}");
        stringBuilder.AppendLine();
        GenerateDelegateExtensionsClass(stringBuilder, info.DelegateSymbol, info.DelegateName, info.DelegateType);

        context.AddSource($"{info.NamespaceName}.{info.DelegateName}.generated.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
    }

    private static void GenerateDelegateExtensionsClass(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string delegateName, DelegateType delegateType)
    {
        stringBuilder.AppendLine($"public static class {delegateName}Extensions");
        stringBuilder.AppendLine("{");

        var parametersList = delegateSymbol.DelegateInvokeMethod!.Parameters.ToList();

        string args = parametersList.Any()
            ? string.Join(", ", parametersList.Select(x => $"{GetRefKindKeyword(x)}{x.Type} {x.Name}"))
            : string.Empty;

        string parameters = parametersList.Any()
            ? string.Join(", ", parametersList.Select(x => $"{GetRefKindKeyword(x)}{x.Name}"))
            : string.Empty;

        string delegateTypeString = delegateType == DelegateType.Multicast ? "TMulticastDelegate" : "TDelegate";

        stringBuilder.AppendLine($"     public static void Invoke(this {delegateTypeString}<{delegateSymbol}> @delegate{(args.Any() ? $", {args}" : string.Empty)})");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         @delegate.InnerDelegate.Invoke({parameters});");
        stringBuilder.AppendLine("     }");
        stringBuilder.AppendLine("}");
    }

    internal static string GetRefKindKeyword(IParameterSymbol x)
    {
        return x.RefKind switch
        {
            RefKind.RefReadOnlyParameter => "in ",
            RefKind.In => "in ",
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            _ => string.Empty
        };
    }
}