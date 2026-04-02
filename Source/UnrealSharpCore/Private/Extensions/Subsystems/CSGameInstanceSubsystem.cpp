#include "Extensions/Subsystems/CSGameInstanceSubsystem.h"

bool UCSGameInstanceSubsystem::K2_ShouldCreateSubsystem_Implementation(UObject* SubsystemOuter) const
{
	return true;
}

void UCSGameInstanceSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	K2_Initialize(Collection);
}

void UCSGameInstanceSubsystem::Deinitialize()
{
	Super::Deinitialize();
	K2_Deinitialize();
}

bool UCSGameInstanceSubsystem::ShouldCreateSubsystem(UObject* Outer) const
{
	if (!Super::ShouldCreateSubsystem(Outer))
	{
		return false;
	}

	return K2_ShouldCreateSubsystem(Outer);
}

void UCSGameInstanceSubsystem::Tick(float DeltaTime)
{
	K2_Tick(DeltaTime);
}

ETickableTickType UCSGameInstanceSubsystem::GetTickableTickType() const
{
	return ETickableTickType::Conditional;
}

bool UCSGameInstanceSubsystem::IsTickable() const
{
	return bIsTickable;
}

TStatId UCSGameInstanceSubsystem::GetStatId() const
{
	RETURN_QUICK_DECLARE_CYCLE_STAT(UCSGameInstanceSubsystem, STATGROUP_Tickables);
}

void UCSGameInstanceSubsystem::SetIsTickable(bool bInIsTickable)
{
	bIsTickable = bInIsTickable;
}

UGameInstance* UCSGameInstanceSubsystem::K2_GetGameInstance() const
{
	return GetGameInstance();
}
