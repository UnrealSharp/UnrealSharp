#include "Extensions/CSEditorSubsystem.h"

bool UCSEditorSubsystem::K2_ShouldCreateSubsystem_Implementation(UObject* SubsystemOuter) const
{
	return true;
}

void UCSEditorSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	K2_Initialize();
}

void UCSEditorSubsystem::Deinitialize()
{
	Super::Deinitialize();
	K2_Deinitialize();
}

bool UCSEditorSubsystem::ShouldCreateSubsystem(UObject* Outer) const
{
	if (!Super::ShouldCreateSubsystem(Outer))
	{
		return false;
	}
  
	return K2_ShouldCreateSubsystem(Outer);
}
