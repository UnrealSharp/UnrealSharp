#include "CSMovementComponentExtensions.h"
#include "GameFramework/MovementComponent.h"

float UCSMovementComponentExtensions::SlideAlongSurface(UMovementComponent* MovementComponent, const FVector& Delta, float Time, const FVector& Normal, UPARAM(ref) FHitResult& Hit, bool bHandleImpact)
{
	return MovementComponent->SlideAlongSurface(Delta, Time, Normal, Hit, bHandleImpact);
}

FVector UCSMovementComponentExtensions::ComputeSlideVector(UMovementComponent* MovementComponent, const FVector& Delta, const float Time, const FVector& Normal, const FHitResult& Hit)
{
	return MovementComponent->ComputeSlideVector(Delta, Time, Normal, Hit);
}

void UCSMovementComponentExtensions::TwoWallAdjust(UMovementComponent* MovementComponent, UPARAM(ref) FVector& Delta, const FHitResult& Hit, const FVector& OldHitNormal)
{
	MovementComponent->TwoWallAdjust(Delta, Hit, OldHitNormal);
}

bool UCSMovementComponentExtensions::SafeMoveUpdatedComponentQuat(UMovementComponent* MovementComponent, const FVector& Delta, const FQuat& NewRotation, bool bSweep, FHitResult& OutHit, ETeleportType Teleport)
{
	return MovementComponent->SafeMoveUpdatedComponent(Delta, NewRotation, bSweep, OutHit, Teleport);
}

bool UCSMovementComponentExtensions::SafeMoveUpdatedComponentRotator(UMovementComponent* MovementComponent, const FVector& Delta, const FRotator& NewRotation, bool bSweep, FHitResult& OutHit, ETeleportType Teleport)
{
	return MovementComponent->SafeMoveUpdatedComponent(Delta, NewRotation, bSweep, OutHit, Teleport);
}

bool UCSMovementComponentExtensions::ResolvePenetrationQuat(UMovementComponent* MovementComponent, const FVector& Adjustment, const FHitResult& Hit, const FQuat& NewRotation)
{
	return MovementComponent->ResolvePenetration(Adjustment, Hit, NewRotation);
}

bool UCSMovementComponentExtensions::ResolvePenetrationRotator(UMovementComponent* MovementComponent, const FVector& Adjustment, const FHitResult& Hit, const FRotator& NewRotation)
{
	return MovementComponent->ResolvePenetration(Adjustment, Hit, NewRotation);
}

void UCSMovementComponentExtensions::UpdateComponentVelocity(UMovementComponent* MovementComponent)
{
	MovementComponent->UpdateComponentVelocity();
}
