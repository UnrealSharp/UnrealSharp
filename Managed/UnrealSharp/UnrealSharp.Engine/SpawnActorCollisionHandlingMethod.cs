namespace UnrealSharp.Engine;

public enum SpawnActorCollisionHandlingMethod : byte
{
    Default,
    AlwaysSpawn,
    AdjustIfPossibleButAlwaysSpawn,
    AdjustIfPossibleButDontSpawnIfColliding,
    DontSpawnIfColliding
}