#include "Extensions/Subsystems/CSLocalPlayerSubsystem.h"

bool UCSLocalPlayerSubsystem::K2_ShouldCreateSubsystem_Implementation(UObject* SubsystemOuter) const
{
	return true;
}

void UCSLocalPlayerSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	K2_Initialize(Collection);
}

void UCSLocalPlayerSubsystem::Deinitialize()
{
	Super::Deinitialize();
	K2_Deinitialize();
}

bool UCSLocalPlayerSubsystem::ShouldCreateSubsystem(UObject* Outer) const
{
	if (!Super::ShouldCreateSubsystem(Outer))
	{
		return false;
	}

	return K2_ShouldCreateSubsystem(Outer);
}

void UCSLocalPlayerSubsystem::PlayerControllerChanged(APlayerController* NewPlayerController)
{
	Super::PlayerControllerChanged(NewPlayerController);
	K2_PlayerControllerChanged(NewPlayerController);
}

void UCSLocalPlayerSubsystem::Tick(float DeltaTime)
{
	K2_Tick(DeltaTime);
}

ETickableTickType UCSLocalPlayerSubsystem::GetTickableTickType() const
{
	return ETickableTickType::Conditional;
}

bool UCSLocalPlayerSubsystem::IsTickable() const
{
	return bIsTickable;
}

TStatId UCSLocalPlayerSubsystem::GetStatId() const
{
	RETURN_QUICK_DECLARE_CYCLE_STAT(UCSLocalPlayerSubsystem, STATGROUP_Tickables);
}

ULocalPlayer* UCSLocalPlayerSubsystem::K2_GetLocalPlayer() const
{
	return GetLocalPlayer();
}
