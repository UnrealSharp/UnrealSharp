using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.ExtensionSourceGenerators;

public class ActorExtensionGenerator : ExtensionGenerator
{
    public override void Generate(ref StringBuilder builder, INamedTypeSymbol classSymbol)
    {
        GenerateSpawnMethod(ref builder, classSymbol);
        GenerateGetActorsOfClassMethod(ref builder, classSymbol);
    }
    
    private void GenerateSpawnMethod(ref StringBuilder stringBuilder, INamedTypeSymbol classSymbol)
    {
        string fullTypeName = classSymbol.ToDisplayString();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Spawns an actor of the specified class.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"worldContextObject\">The object to spawn the actor in.</param>");
        stringBuilder.AppendLine("     /// <param name=\"actorClass\">The class of the actor to spawn.</param>");
        stringBuilder.AppendLine("     /// <param name=\"spawnTransform\">The transform to spawn the actor at.</param>");
        stringBuilder.AppendLine("     /// <param name=\"spawnMethod\">The method to handle collisions when spawning the actor.</param>");
        stringBuilder.AppendLine("     /// <param name=\"instigator\">The actor that caused the actor to be spawned.</param>");
        stringBuilder.AppendLine("     /// <param name=\"owner\">The actor that owns the spawned actor.</param>");
        stringBuilder.AppendLine("     /// <returns>The spawned actor.</returns>");
        stringBuilder.AppendLine($"     public static {fullTypeName} Spawn(UnrealSharp.CoreUObject.Object worldContextObject, SubclassOf<{fullTypeName}> actorClass = default, Transform spawnTransform = default, UnrealSharp.Engine.ESpawnActorCollisionHandlingMethod spawnMethod = ESpawnActorCollisionHandlingMethod.Undefined, Pawn? instigator = null, Actor? owner = null)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         return worldContextObject.SpawnActor<{fullTypeName}>(actorClass, spawnTransform, spawnMethod, instigator, owner);");
        stringBuilder.AppendLine("     }");
    }
    
    private void GenerateGetActorsOfClassMethod(ref StringBuilder stringBuilder, INamedTypeSymbol classSymbol)
    {
        string fullTypeName = classSymbol.ToDisplayString();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("     /// <summary>");
        stringBuilder.AppendLine("     /// Gets all actors of the specified class in the world.");
        stringBuilder.AppendLine("     /// </summary>");
        stringBuilder.AppendLine("     /// <param name=\"worldContextObject\">The object to get the actors from.</param>");
        stringBuilder.AppendLine("     /// <param name=\"outActors\">The list to store the actors in.</param>");
        stringBuilder.AppendLine("     public static new void GetAllActorsOfClass(UnrealSharp.CoreUObject.Object worldContextObject, out IList<Actor> outActors)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         GameplayStatics.GetAllActorsOfClass(worldContextObject, typeof({fullTypeName}), out outActors);");
        stringBuilder.AppendLine("     }");
    }
}