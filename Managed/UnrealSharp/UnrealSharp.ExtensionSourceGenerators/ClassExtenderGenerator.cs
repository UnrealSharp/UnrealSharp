using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.SourceGenerator.Utilities;

namespace UnrealSharp.ExtensionSourceGenerators;

public enum ExtensionType
{
    Unknown,
    Actor,
    ActorComponent
}
    
public readonly record struct ParseResult
{
    public readonly string TypeName;
    public readonly string TypeNamespace;
    public string FullTypeName => $"{TypeNamespace}.{TypeName}";
    public readonly ExtensionType ExtensionType;
    
    public bool IsEmpty => string.IsNullOrEmpty(TypeName);
        
    public ParseResult(string typeName, string typeNamespace, ExtensionType extensionType)
    {
        TypeName = typeName;
        TypeNamespace = typeNamespace;
        ExtensionType = extensionType;
    }
    
    public static ParseResult Empty => new ParseResult(string.Empty, string.Empty, ExtensionType.Unknown);
}

[Generator]
public class ClassExtenderGenerator : IIncrementalGenerator
{
    private record struct GeneratorInfo(ExtensionType ExtensionType, ExtensionGenerator Generator);
    
    private readonly List<GeneratorInfo> _generators =
    [
        new(ExtensionType.Actor, new ActorExtensionGenerator()),
        new(ExtensionType.ActorComponent, new ActorComponentExtensionGenerator())
    ];
    
    private ExtensionGenerator GetGenerator(ExtensionType extensionType)
    {
        foreach (GeneratorInfo generatorInfo in _generators)
        {
            if (generatorInfo.ExtensionType == extensionType)
            {
                return generatorInfo.Generator;
            }
        }

        throw new System.Exception($"No generator found for extension type {extensionType}.");
    }
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ParseResult> discoveryResults = context.SyntaxProvider.ForAttributeWithMetadataName(
            "UnrealSharp.Attributes.UClassAttribute", Predicate, GetResult);
        
        discoveryResults = discoveryResults.Where(result => !result.IsEmpty);

        context.RegisterSourceOutput(discoveryResults, (outputContext, parseResult) =>
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("#nullable disable");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("using UnrealSharp.Engine;");
            stringBuilder.AppendLine("using UnrealSharp.CoreUObject;");
            stringBuilder.AppendLine("using UnrealSharp;");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"namespace {parseResult.TypeNamespace};");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"public partial class {parseResult.TypeName}");
            stringBuilder.AppendLine("{");

            ExtensionGenerator generator = GetGenerator(parseResult.ExtensionType);
            generator.Generate(stringBuilder, parseResult);

            stringBuilder.AppendLine("}");
            outputContext.AddSource($"{parseResult.TypeName}.generated.extension.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
        });
    }

    private ParseResult GetResult(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        ITypeSymbol typeSymbol = (ITypeSymbol)context.TargetSymbol;

        if (typeSymbol.InheritsFrom("AActor"))
        {
            return new ParseResult(typeSymbol.Name, typeSymbol.ContainingNamespace.ToDisplayString(), ExtensionType.Actor);
        }

        if (typeSymbol.InheritsFrom("UActorComponent"))
        {
            return new ParseResult(typeSymbol.Name, typeSymbol.ContainingNamespace.ToDisplayString(), ExtensionType.ActorComponent);
        }
        
        return ParseResult.Empty;
    }

    private bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return true;
    }
}