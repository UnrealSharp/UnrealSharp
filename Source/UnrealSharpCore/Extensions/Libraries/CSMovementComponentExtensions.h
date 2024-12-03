#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSMovementComponentExtensions.generated.h"

class UMovementComponent;

UCLASS(meta = (Internal))
class UCSMovementComponentExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	/**
	 * Slide smoothly along a surface, and slide away from multiple impacts using TwoWallAdjust if necessary. Calls HandleImpact for each surface hit, if requested.
	 * Uses SafeMoveUpdatedComponent() for movement, and ComputeSlideVector() to determine the slide direction.
	 * @param Delta:	Attempted movement vector.
	 * @param Time:		Percent of Delta to apply (between 0 and 1). Usually equal to the remaining time after a collision: (1.0 - Hit.Time).
	 * @param Normal:	Normal opposing movement, along which we will slide.
	 * @param Hit:		[In] HitResult of the attempted move that resulted in the impact triggering the slide. [Out] HitResult of last attempted move.
	 * @param bHandleImpact:	Whether to call HandleImpact on each hit.
	 * @return The percentage of requested distance (Delta * Percent) actually applied (between 0 and 1). 0 if no movement occurred, non-zero if movement occurred.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static float SlideAlongSurface(UMovementComponent* MovementComponent, const FVector& Delta, float Time, const FVector& Normal, UPARAM(ref) FHitResult& Hit, bool bHandleImpact = false);

	/**
	 * Compute a vector to slide along a surface, given an attempted move, time, and normal.
	 * @param Delta:	Attempted move.
	 * @param Time:		Amount of move to apply (between 0 and 1).
	 * @param Normal:	Normal opposed to movement. Not necessarily equal to Hit.Normal.
	 * @param Hit:		HitResult of the move that resulted in the slide.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static FVector ComputeSlideVector(UMovementComponent* MovementComponent, const FVector& Delta, const float Time, const FVector& Normal, const FHitResult& Hit);

	/**
	 * Compute a movement direction when contacting two surfaces.
	 * @param Delta:		[In] Amount of move attempted before impact. [Out] Computed adjustment based on impacts.
	 * @param Hit:			Impact from last attempted move
	 * @param OldHitNormal:	Normal of impact before last attempted move
	 * @return Result in Delta that is the direction to move when contacting two surfaces.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static void TwoWallAdjust(UMovementComponent* MovementComponent, UPARAM(ref) FVector& Delta, const FHitResult& Hit, const FVector& OldHitNormal);

	/**
	 * Calls MoveUpdatedComponent(), handling initial penetrations by calling ResolvePenetration().
	 * If this adjustment succeeds, the original movement will be attempted again.
	 * @note The overload taking rotation as an FQuat is slightly faster than the version using FRotator (which will be converted to an FQuat).
	 * @note The 'Teleport' flag is currently always treated as 'None' (not teleporting) when used in an active FScopedMovementUpdate.
	 * @return result of the final MoveUpdatedComponent() call.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static bool SafeMoveUpdatedComponentQuat(UMovementComponent* MovementComponent, const FVector& Delta, const FQuat& NewRotation, bool bSweep, UPARAM(ref) FHitResult& OutHit, ETeleportType Teleport = ETeleportType::None);

	/**
	 * Calls MoveUpdatedComponent(), handling initial penetrations by calling ResolvePenetration().
	 * If this adjustment succeeds, the original movement will be attempted again.
	 * @note The overload taking rotation as an FQuat is slightly faster than the version using FRotator (which will be converted to an FQuat).
	 * @note The 'Teleport' flag is currently always treated as 'None' (not teleporting) when used in an active FScopedMovementUpdate.
	 * @return result of the final MoveUpdatedComponent() call.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static bool SafeMoveUpdatedComponentRotator(UMovementComponent* MovementComponent, const FVector& Delta, const FRotator& NewRotation, bool bSweep, UPARAM(ref) FHitResult& OutHit, ETeleportType Teleport = ETeleportType::None);

	/**
	 * Try to move out of penetration in an object after a failed move. This function should respect the plane constraint if applicable.
	 * @note This simply calls the virtual ResolvePenetrationImpl() which can be overridden to implement custom behavior.
	 * @note The overload taking rotation as an FQuat is slightly faster than the version using FRotator (which will be converted to an FQuat)..
	 * @param Adjustment	The requested adjustment, usually from GetPenetrationAdjustment()
	 * @param Hit			The result of the failed move
	 * @return True if the adjustment was successful and the original move should be retried, or false if no repeated attempt should be made.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static bool ResolvePenetrationQuat(UMovementComponent* MovementComponent, const FVector& Adjustment, const FHitResult& Hit, const FQuat& NewRotation);

	/**
	 * Try to move out of penetration in an object after a failed move. This function should respect the plane constraint if applicable.
	 * @note This simply calls the virtual ResolvePenetrationImpl() which can be overridden to implement custom behavior.
	 * @note The overload taking rotation as an FQuat is slightly faster than the version using FRotator (which will be converted to an FQuat)..
	 * @param Adjustment	The requested adjustment, usually from GetPenetrationAdjustment()
	 * @param Hit			The result of the failed move
	 * @return True if the adjustment was successful and the original move should be retried, or false if no repeated attempt should be made.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static bool ResolvePenetrationRotator(UMovementComponent* MovementComponent, const FVector& Adjustment, const FHitResult& Hit, const FRotator& NewRotation);

	/** Update ComponentVelocity of UpdatedComponent. This needs to be called by derived classes at the end of an update whenever Velocity has changed.	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static void UpdateComponentVelocity(UMovementComponent* MovementComponent);

};
