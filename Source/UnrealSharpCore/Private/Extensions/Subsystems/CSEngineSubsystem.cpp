#include "Extensions/Subsystems/CSEngineSubsystem.h"

bool UCSEngineSubsystem::K2_ShouldCreateSubsystem_Implementation(UObject* SubsystemOuter) const
{
	return true;
}

void UCSEngineSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	K2_Initialize(Collection);
}

void UCSEngineSubsystem::Deinitialize()
{
	Super::Deinitialize();
	K2_Deinitialize();
}

bool UCSEngineSubsystem::ShouldCreateSubsystem(UObject* Outer) const
{
	if (!Super::ShouldCreateSubsystem(Outer))
	{
		return false;
	}

	return K2_ShouldCreateSubsystem(Outer);
}

void UCSEngineSubsystem::Tick(float DeltaTime)
{
	K2_Tick(DeltaTime);
}

ETickableTickType UCSEngineSubsystem::GetTickableTickType() const
{
	return ETickableTickType::Conditional;
}

bool UCSEngineSubsystem::IsTickable() const
{
	return bIsTickable;
}

TStatId UCSEngineSubsystem::GetStatId() const
{
	RETURN_QUICK_DECLARE_CYCLE_STAT(UCSEngineSubsystem, STATGROUP_Tickables);
}
