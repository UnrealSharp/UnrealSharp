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
        
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Constructs a new component of the specified class, and attaches it to the specified actor.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"owner\">The actor to attach the component to.</param>");
        stringBuilder.AppendLine("     /// <param name=\"bManualAttachment\">If true, the component will not be attached to the actor's root component.</param>");
        stringBuilder.AppendLine("     /// <param name=\"relativeTransform\">The relative transform of the component to the actor.</param>");
        stringBuilder.AppendLine("     /// <returns>The constructed component.</returns>");
        stringBuilder.AppendLine($"     public static {fullTypeName} Construct(UnrealSharp.Engine.Actor owner, bool bManualAttachment, Transform relativeTransform)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         return owner.AddComponentByClass<{fullTypeName}>(bManualAttachment, relativeTransform);");
        stringBuilder.AppendLine("     }");
        
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Constructs a new component of the specified class, and attaches it to the specified actor.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"owner\">The actor to attach the component to.</param>");
        stringBuilder.AppendLine("     /// <param name=\"componentClass\">The class of the component to construct.</param>");
        stringBuilder.AppendLine("     /// <param name=\"bManualAttachment\">If true, the component will not be attached to the actor's root component.</param>");
        stringBuilder.AppendLine("     /// <param name=\"relativeTransform\">The relative transform of the component to the actor.</param>");
        stringBuilder.AppendLine("     /// <returns>The constructed component.</returns>");
        stringBuilder.AppendLine($"     public static {fullTypeName} Construct(UnrealSharp.Engine.Actor owner, SubclassOf<ActorComponent> componentClass, bool bManualAttachment, Transform relativeTransform)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         return ({fullTypeName}) owner.AddComponentByClass(componentClass, bManualAttachment, relativeTransform);");
        stringBuilder.AppendLine("     }");
        
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Constructs a new component of the specified class, and attaches it to the specified actor.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"owner\">The actor to attach the component to.</param>");
        stringBuilder.AppendLine("     /// <returns>The constructed component.</returns>");
        stringBuilder.AppendLine($"     public static {fullTypeName} Construct(UnrealSharp.Engine.Actor owner)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         return ({fullTypeName}) owner.AddComponentByClass(typeof({fullTypeName}), false, new Transform());");
        stringBuilder.AppendLine("     }");
    }
    
    private void GenerateComponentGetter(ref StringBuilder stringBuilder, INamedTypeSymbol classSymbol)
    {
        string fullTypeName = classSymbol.ToDisplayString();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Gets the component of the specified class attached to the specified actor.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"owner\">The actor to get the component from.</param>");
        stringBuilder.AppendLine("     /// <returns>The component if found, otherwise null.</returns>");
        stringBuilder.AppendLine($"     public static new {fullTypeName}? Get(UnrealSharp.Engine.Actor owner)");
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