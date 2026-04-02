using System.Text;

namespace UnrealSharp.ExtensionSourceGenerators;

public record ActorComponentExtensionGenerator : ExtensionGenerator
{
    public override void Generate(StringBuilder builder, ParseResult parseResult)
    {
        string fullTypeName = parseResult.FullTypeName;
        GenerateConstructMethod(builder, fullTypeName);
        GenerateComponentGetter(builder, fullTypeName);
    }
    
    private void GenerateConstructMethod(StringBuilder stringBuilder, string fullTypeName)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Constructs a new component of the specified class, and attaches it to the specified actor.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"owner\">The actor to attach the component to.</param>");
        stringBuilder.AppendLine("     /// <param name=\"bManualAttachment\">If true, the component will not be attached to the actor's root component.</param>");
        stringBuilder.AppendLine("     /// <param name=\"relativeTransform\">The relative transform of the component to the actor.</param>");
        stringBuilder.AppendLine("     /// <returns>The constructed component.</returns>");
        stringBuilder.AppendLine($"     public static {fullTypeName} Construct(UnrealSharp.Engine.AActor owner, bool bManualAttachment, FTransform relativeTransform)");
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
        stringBuilder.AppendLine($"     public static {fullTypeName} Construct(UnrealSharp.Engine.AActor owner, TSubclassOf<UActorComponent> componentClass, bool bManualAttachment, FTransform relativeTransform)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         return ({fullTypeName}) owner.AddComponentByClass(componentClass, bManualAttachment, relativeTransform);");
        stringBuilder.AppendLine("     }");
        
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Constructs a new component of the specified class, and attaches it to the specified actor.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"owner\">The actor to attach the component to.</param>");
        stringBuilder.AppendLine("     /// <returns>The constructed component.</returns>");
        stringBuilder.AppendLine($"     public static {fullTypeName} Construct(UnrealSharp.Engine.AActor owner)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         return ({fullTypeName}) owner.AddComponentByClass(typeof({fullTypeName}), false, new FTransform());");
        stringBuilder.AppendLine("     }");
    }
    
    private void GenerateComponentGetter(StringBuilder stringBuilder, string fullTypeName)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Gets the component of the specified class attached to the specified actor.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"owner\">The actor to get the component from.</param>");
        stringBuilder.AppendLine("     /// <returns>The component if found, otherwise null.</returns>");
        stringBuilder.AppendLine($"     public static new {fullTypeName}? Get(UnrealSharp.Engine.AActor owner)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"        UActorComponent? foundComponent = owner.GetComponentByClass<{fullTypeName}>(typeof({fullTypeName}));");
        stringBuilder.AppendLine("        if (foundComponent != null)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine($"            return ({fullTypeName}) foundComponent;");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        return null;");
        stringBuilder.AppendLine("     }");
    }
}