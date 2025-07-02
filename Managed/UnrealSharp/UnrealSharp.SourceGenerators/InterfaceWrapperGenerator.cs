using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.SourceGenerators;

[Generator]
public class InterfaceWrapperGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var interfaceProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (n, _) => n is InterfaceDeclarationSyntax,
            (ctx, _) =>
            {
                var interfaceDeclaration = (InterfaceDeclarationSyntax)ctx.Node;
                var interfaceSymbol = ctx.SemanticModel.GetDeclaredSymbol(interfaceDeclaration) as INamedTypeSymbol;

                if (interfaceSymbol is null || !interfaceSymbol.GetAttributes().Any(attr =>
                        attr.AttributeClass?.Name == AnalyzerStatics.UInterfaceAttribute))
                {
                    return null;
                }

                return interfaceSymbol;

            })
            .Where(x => x != null);
        
        context.RegisterSourceOutput(interfaceProvider, Execute!);
    }

    private static void Execute(SourceProductionContext context, INamedTypeSymbol interfaceSymbol)
    {
        /*
        var sourceBuilder = new SourceBuilder();
        
        sourceBuilder.AppendLine("using UnrealSharp;")
            .AppendLine("using UnrealSharp.CoreUObject;")
            .AppendLine("")
            .AppendLine($"namespace {interfaceSymbol.ContainingNamespace.ToDisplayString()}");

        using (sourceBuilder.OpenBlock())
        {
            sourceBuilder.AppendLine($"internal sealed partial class {interfaceSymbol.Name}Wrapper : {interfaceSymbol.Name}, IScriptInterface");
            using var marshallerClassBody = sourceBuilder.OpenBlock();

            sourceBuilder.AppendLine()
                .AppendLine("public UObject Object { get; }")
                .AppendLine("private IntPtr NativeObject => Object.NativeObject;")
                .AppendLine();

            sourceBuilder.AppendLine($"internal {interfaceSymbol.Name}Wrapper(UObject wrappedObject)");
            using (sourceBuilder.OpenBlock())
            {
                sourceBuilder.AppendLine("Object = wrappedObject;");
            }
            sourceBuilder.AppendLine();

            foreach (var property in interfaceSymbol.GetMembers()
                         .OfType<IPropertySymbol>()
                         .Where(p => !p.IsStatic))
            {
                sourceBuilder.AppendLine($"public {property.Type} {property.Name}");
                using var propertyBody = sourceBuilder.OpenBlock();
                if (property.GetMethod is not null)
                {
                    sourceBuilder.AppendLine($"get => throw new NotImplementedException();");
                }
                if (property.SetMethod is not null)
                {
                    sourceBuilder.AppendLine($"get => throw new NotImplementedException();");
                }
            }
            
            foreach (var method in interfaceSymbol.GetMembers()
                         .OfType<IMethodSymbol>()
                         .Where(m => !m.IsStatic))
            {
                sourceBuilder.AppendLine($"public {method.ReturnType} {method.Name}({string.Join(", ", method.Parameters.Select(x => $"{(x.RefKind == RefKind.None ? "" : x.RefKind.ToString().ToLowerInvariant() + " ")}{(x.IsParams ? "params " : "")}{x.Type} {x.Name}"))})");
                using var methodBody = sourceBuilder.OpenBlock();
                sourceBuilder.AppendLine("throw new NotImplementedException();");
            }
        }
        
        context.AddSource($"{interfaceSymbol.Name}Wrapper.generated.cs", sourceBuilder.ToString());
        */
    }

}