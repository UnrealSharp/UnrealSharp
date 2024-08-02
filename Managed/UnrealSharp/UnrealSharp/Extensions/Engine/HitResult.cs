namespace UnrealSharp.Engine;

public partial struct FHitResult
{
    /// <summary>
    /// The hit Actor.
    /// </summary>
    public AActor? Actor => BlockingHit ? Component.Object!.GetOwner() : null;
}