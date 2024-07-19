namespace UnrealSharp.Engine;

public partial struct HitResult
{
    /// <summary>
    /// The hit Actor.
    /// </summary>
    public Actor? Actor => BlockingHit ? Component.Object!.GetOwner() : null;
}