#include "Extensions/Subsystems/CSWorldSubsystem.h"
#include "Extensions/Subsystems/SubsystemUtils.h"

bool UCSWorldSubsystem::K2_ShouldCreateSubsystem_Implementation(UObject* SubsystemOuter) const
{
	return true;
}

void UCSWorldSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	K2_Initialize(Collection);
}

void UCSWorldSubsystem::Deinitialize()
{
	if (IsInitialized())
	{
		Super::Deinitialize();
		K2_Deinitialize();
	}
}

bool UCSWorldSubsystem::ShouldCreateSubsystem(UObject* Outer) const
{
	if (!Super::ShouldCreateSubsystem(Outer))
	{
		return false;
	}

	return K2_ShouldCreateSubsystem(Outer);
}

bool UCSWorldSubsystem::DoesSupportWorldType(const EWorldType::Type WorldType) const
{
	if (!Super::DoesSupportWorldType(WorldType))
	{
		return false;
	}

	return K2_DoesSupportWorldType(static_cast<ECSWorldType>(WorldType));
}

void UCSWorldSubsystem::BeginDestroy()
{
#if ENGINE_MAJOR_VERSION >= 5 && ENGINE_MINOR_VERSION >= 5
#if WITH_EDITOR
	// Edge case in reinstancing world subsystems. Can't call Super as it leads to an ensure, but we do the same thing.
	if (FCSSubsystemUtils::IsReinstancingClass(GetClass()))
	{
		SetTickableTickType(ETickableTickType::Never);
		UObject::BeginDestroy();
		return;
	}
#endif
#endif
	Super::BeginDestroy();
}

void UCSWorldSubsystem::PostInitialize()
{
	Super::PostInitialize();
	K2_PostInitialize();
}

void UCSWorldSubsystem::OnWorldBeginPlay(UWorld& InWorld)
{
	Super::OnWorldBeginPlay(InWorld);
	K2_OnWorldBeginPlay();
}

void UCSWorldSubsystem::OnWorldComponentsUpdated(UWorld& World)
{
	Super::OnWorldComponentsUpdated(World);
	K2_OnWorldComponentsUpdated();
}

TStatId UCSWorldSubsystem::GetStatId() const
{
	RETURN_QUICK_DECLARE_CYCLE_STAT(UCSWorldSubsystem, STATGROUP_Tickables);
}

void UCSWorldSubsystem::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
	K2_Tick(DeltaTime);
}

bool UCSWorldSubsystem::GetIsInitialized() const
{
	return IsInitialized();
}

bool UCSWorldSubsystem::K2_DoesSupportWorldType_Implementation(const ECSWorldType WorldType) const
{
    return true;
}
