#include "CSWorldSubsystem.h"
#include "SubsystemUtils.h"

bool UCSWorldSubsystem::K2_ShouldCreateSubsystem_Implementation() const
{
	return true;
}

void UCSWorldSubsystem::BeginDestroy()
{
#if ENGINE_MINOR_VERSION >= 5
#if WITH_EDITOR
	// Edge case in reinstancing world subsystems. Can't call Super as it leads to an ensure, but we do the same thing.
	if (CSSubsystemUtils::IsReinstancingClass(GetClass()))
	{
		SetTickableTickType(ETickableTickType::Never);
		UObject::BeginDestroy();
		return;
	}
#endif
#endif
	Super::BeginDestroy();
}

bool UCSWorldSubsystem::GetIsInitialized() const
{
	return IsInitialized();
}

bool UCSWorldSubsystem::K2_DoesSupportWorldType_Implementation(const ECSWorldType WorldType) const
{
    return true;
}
