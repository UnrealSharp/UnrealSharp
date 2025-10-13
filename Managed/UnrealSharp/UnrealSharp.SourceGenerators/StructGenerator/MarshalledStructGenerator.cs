using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.SourceGenerators.StructGenerator;

[Generator]
public class MarshalledStructGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider((n, _) => n is StructDeclarationSyntax or RecordDeclarationSyntax,
                (ctx, _) =>
                {
                    var structDeclaration = ctx.Node;
                    if (ctx.SemanticModel.GetDeclaredSymbol(structDeclaration) is not INamedTypeSymbol { TypeKind: TypeKind.Struct } structSymbol)
                    {
                        return null;
                    }

                    return AnalyzerStatics.HasAttribute(structSymbol, AnalyzerStatics.UStructAttribute)
                           && !structSymbol.Interfaces.Any(i => i.MetadataName == "MarshalledStruct`1")
                           && structSymbol.DeclaringSyntaxReferences
                               .Select(r => r.GetSyntax())
                               .OfType<TypeDeclarationSyntax>()
                               .SelectMany(s => s.Modifiers)
                               .Any(m => m.IsKind(SyntaxKind.PartialKeyword))
                        ? structSymbol
                        : null;
                })
            .Where(sym => sym is not null);

        context.RegisterSourceOutput(syntaxProvider, GenerateStruct!);
    }

    private static void GenerateStruct(SourceProductionContext context, INamedTypeSymbol structSymbol)
    {
        using var builder = new SourceBuilder();

        WriteStructCode(builder, structSymbol);

        context.AddSource($"{structSymbol.Name}.generated.cs", builder.ToString());
    }

    private static void WriteStructCode(SourceBuilder builder, INamedTypeSymbol structSymbol)
    {
        builder.AppendLine("using UnrealSharp;");
        builder.AppendLine("using UnrealSharp.Attributes;");
        builder.AppendLine("using UnrealSharp.Core.Marshallers;");
        builder.AppendLine("using UnrealSharp.Interop;");
        builder.AppendLine();
        builder.AppendLine($"namespace {structSymbol.ContainingNamespace.ToDisplayString()};");
        builder.AppendLine();

        builder.AppendLine(
            $"partial {(structSymbol.IsRecord ? "record " : "")}struct {structSymbol.Name} : MarshalledStruct<{structSymbol.Name}>");

        using var structScope = builder.OpenBlock();

        builder.AppendLine("[WeaverGenerated]");
        builder.AppendLine("public static extern IntPtr GetNativeClassPtr();");
        builder.AppendLine();
        builder.AppendLine("[WeaverGenerated]");
        builder.AppendLine("public static extern int GetNativeDataSize();");
        builder.AppendLine();
        builder.AppendLine("[WeaverGenerated]");
        builder.AppendLine($"public static extern {structSymbol.Name} FromNative(IntPtr InNativeStruct);");
        builder.AppendLine();
        builder.AppendLine("[WeaverGenerated]");
        builder.AppendLine("public extern void ToNative(IntPtr buffer);");
    }
}

    