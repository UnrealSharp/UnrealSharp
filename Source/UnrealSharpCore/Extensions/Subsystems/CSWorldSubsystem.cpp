#include "CSWorldSubsystem.h"

bool UCSWorldSubsystem::K2_ShouldCreateSubsystem_Implementation() const
{
	return true;
}

bool UCSWorldSubsystem::GetIsInitialized() const
{
	return IsInitialized();
}
