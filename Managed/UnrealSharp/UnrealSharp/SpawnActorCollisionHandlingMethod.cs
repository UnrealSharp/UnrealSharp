namespace UnrealSharp;
public enum SpawnActorCollisionHandlingMethod : byte
{
    Default,
    AlwaysSpawn,
    AdjustIfPossibleButAlwaysSpawn,
    AdjustIfPossibleButDontSpawnIfColliding,
    DontSpawnIfColliding
}