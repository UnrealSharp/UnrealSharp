using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnrealSharp.CoreUObject;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.Engine;

public partial class UMovementComponent : UnrealSharp.Engine.UActorComponent
{
    /// <summary>
    /// Update ComponentVelocity of UpdatedComponent. This needs to be called by derived classes at the end of an update whenever Velocity has changed.	 */
    /// </summary>
    public void UpdateComponentVelocity()
    {
        UCSMovementComponentExtensions.UpdateComponentVelocity(this);
    }

    /// <summary>
    /// Slide smoothly along a surface, and slide away from multiple impacts using TwoWallAdjust if necessary. Calls HandleImpact for each surface hit, if requested.
    /// Uses SafeMoveUpdatedComponent() for movement, and ComputeSlideVector() to determine the slide direction.
    /// </summary>
    /// <param name="delta">Attempted movement vector.</param>
    /// <param name="time">Percent of Delta to apply (between 0 and 1). Usually equal to the remaining time after a collision: (1.0 - Hit.Time).</param>
    /// <param name="normal">Normal opposing movement, along which we will slide.</param>
    /// <param name="hit">[In] HitResult of the attempted move that resulted in the impact triggering the slide. [Out] HitResult of last attempted move.</param>
    /// <param name="handleImpact">Whether to call HandleImpact on each hit.</param>
    /// <returns>The percentage of requested distance (Delta * Percent) actually applied (between 0 and 1). 0 if no movement occurred, non-zero if movement occurred.</returns>
    public float SlideAlongSurface(UnrealSharp.CoreUObject.FVector delta, float time, UnrealSharp.CoreUObject.FVector normal, ref FHitResult hit, bool handleImpact = false)
    {
        return UCSMovementComponentExtensions.SlideAlongSurface(this, delta, time, normal, ref hit, handleImpact);
    }

    /// <summary>
    /// Compute a vector to slide along a surface, given an attempted move, time, and normal.
    /// </summary>
    /// <param name="delta">Attempted move.</param>
    /// <param name="time">Amount of move to apply (between 0 and 1).</param>
    /// <param name="normal">Normal opposed to movement. Not necessarily equal to Hit.Normal.</param>
    /// <param name="hit">HitResult of the move that resulted in the slide.</param>
    public UnrealSharp.CoreUObject.FVector ComputeSlideVector(UnrealSharp.CoreUObject.FVector delta, float time, UnrealSharp.CoreUObject.FVector normal, FHitResult hit)
    {
        return UCSMovementComponentExtensions.ComputeSlideVector(this, delta, time, normal, hit);
    }

    /// <summary>
    /// Compute a movement direction when contacting two surfaces.
    /// </summary>
    /// <param name="delta">[In] Amount of move attempted before impact. [Out] Computed adjustment based on impacts.</param>
    /// <param name="hit">Impact from last attempted move</param>
    /// <param name="oldHitNormal">Normal of impact before last attempted move</param>
    public void TwoWallAdjust(ref FVector delta, FHitResult hit, FVector oldHitNormal)
    {
        UCSMovementComponentExtensions.TwoWallAdjust(this, ref delta, hit, oldHitNormal);
    }

    /// <summary>
    /// Calls MoveUpdatedComponent(), handling initial penetrations by calling ResolvePenetration().
    /// If this adjustment succeeds, the original movement will be attempted again.
    /// </summary>
    /// <remarks>The overload taking rotation as an FQuat is slightly faster than the version using FRotator (which will be converted to an FQuat).</remarks>
    /// <remarks>The 'Teleport' flag is currently always treated as 'None' (not teleporting) when used in an active FScopedMovementUpdate.</remarks>
    /// <returns> result of the final MoveUpdatedComponent() call.</returns>
    public bool SafeMoveUpdatedComponent(FVector delta, FQuat newRotation, bool bSweep, ref FHitResult OutHit, ETeleportType Teleport = ETeleportType.None)
    {
        return UCSMovementComponentExtensions.SafeMoveUpdatedComponentQuat(this, delta, newRotation, bSweep, ref OutHit, Teleport);
    }

    /// <summary>
    /// Calls MoveUpdatedComponent(), handling initial penetrations by calling ResolvePenetration().
    /// If this adjustment succeeds, the original movement will be attempted again.
    /// </summary>
    
    /// <remarks>The 'Teleport' flag is currently always treated as 'None' (not teleporting) when used in an active FScopedMovementUpdate.</remarks>
    /// <returns> result of the final MoveUpdatedComponent() call.</returns>
    public bool SafeMoveUpdatedComponent(FVector delta, FRotator newRotation, bool bSweep, ref FHitResult OutHit, ETeleportType Teleport = ETeleportType.None)
    {
        return UCSMovementComponentExtensions.SafeMoveUpdatedComponentRotator(this, delta, newRotation, bSweep, ref OutHit, Teleport);
    }

    /// <summary>
    /// Try to move out of penetration in an object after a failed move. This function should respect the plane constraint if applicable.
    /// </summary>
    /// <remarks>This simply calls the virtual ResolvePenetrationImpl() which can be overridden to implement custom behavior.</remarks>
    /// <remarks>The overload taking rotation as an FQuat is slightly faster than the version using FRotator (which will be converted to an FQuat)..</remarks>
    /// <param name="adjustment">The requested adjustment, usually from GetPenetrationAdjustment()</param>
    /// <param name="hit">The result of the failed move</param>
    /// <returns>True if the adjustment was successful and the original move should be retried, or false if no repeated attempt should be made.</returns>
    public bool ResolvePenetration(FVector adjustment, FHitResult hit, FQuat newRotation)
    {
        return UCSMovementComponentExtensions.ResolvePenetrationQuat(this, adjustment, hit, newRotation);
    }

    /// <summary>
    /// Try to move out of penetration in an object after a failed move. This function should respect the plane constraint if applicable.
    /// </summary>
    /// <remarks>This simply calls the virtual ResolvePenetrationImpl() which can be overridden to implement custom behavior.</remarks>
    /// <remarks>The overload taking rotation as an FQuat is slightly faster than the version using FRotator (which will be converted to an FQuat)..</remarks>
    /// <param name="adjustment">The requested adjustment, usually from GetPenetrationAdjustment()</param>
    /// <param name="hit">The result of the failed move</param>
    /// <returns>True if the adjustment was successful and the original move should be retried, or false if no repeated attempt should be made.</returns>
    public bool ResolvePenetration(FVector adjustment, FHitResult hit, FRotator newRotation)
    {
        return UCSMovementComponentExtensions.ResolvePenetrationRotator(this, adjustment, hit, newRotation);
    }
}