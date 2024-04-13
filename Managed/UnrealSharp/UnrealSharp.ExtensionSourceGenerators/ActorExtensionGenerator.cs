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
        stringBuilder.AppendLine($"     public static {fullTypeName} Spawn(CoreUObject.Object worldContextObject, SubclassOf<{fullTypeName}> actorClass = default, Transform spawnTransform = default, SpawnActorCollisionHandlingMethod spawnMethod = SpawnActorCollisionHandlingMethod.Default, Pawn? instigator = null, Actor? owner = null)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         return worldContextObject.SpawnActor<{fullTypeName}>(actorClass, spawnTransform, spawnMethod, instigator, owner);");
        stringBuilder.AppendLine("     }");
    }
    
    private void GenerateGetActorsOfClassMethod(ref StringBuilder stringBuilder, INamedTypeSymbol classSymbol)
    {
        string fullTypeName = classSymbol.ToDisplayString();
        stringBuilder.AppendLine("     public static new void GetAllActorsOfClass(CoreUObject.Object worldContextObject, out IList<Actor> outActors)");
        stringBuilder.AppendLine("     {");
        stringBuilder.AppendLine($"         GameplayStatics.GetAllActorsOfClass(worldContextObject, typeof({fullTypeName}), out outActors);");
        stringBuilder.AppendLine("     }");
    }
}