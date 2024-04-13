using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.ExtensionSourceGenerators;

public class ActorComponentExtensionGenerator : ExtensionGenerator
{
    public override void Generate(ref StringBuilder builder, INamedTypeSymbol classSymbol)
    {
        GenerateConstructMethod(ref builder, classSymbol);
        GenerateComponentGetter(ref builder, classSymbol);
    }
    
    private void GenerateConstructMethod(ref StringBuilder stringBuilder, INamedTypeSymbol classSymbol)
    {
        string fullTypeName = classSymbol.ToDisplayString();
        stringBuilder.AppendLine($"     public static {fullTypeName} Construct(Actor owner, SubclassOf<{fullTypeName}>? componentClass = null)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         return new {fullTypeName}();");
        stringBuilder.AppendLine("     }");
    }
    
    private void GenerateComponentGetter(ref StringBuilder stringBuilder, INamedTypeSymbol classSymbol)
    {
        string fullTypeName = classSymbol.ToDisplayString();
        stringBuilder.AppendLine($"     public static new {fullTypeName}? Get(Actor owner)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"        ActorComponent? foundComponent = owner.GetComponentByClass(typeof({fullTypeName}));");
        stringBuilder.AppendLine("        if (foundComponent != null)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine($"            return ({fullTypeName}) foundComponent;");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        return null;");
        stringBuilder.AppendLine("     }");
    }
}