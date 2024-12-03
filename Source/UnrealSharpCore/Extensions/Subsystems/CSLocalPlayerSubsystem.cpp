#include "CSLocalPlayerSubsystem.h"

bool UCSLocalPlayerSubsystem::K2_ShouldCreateSubsystem_Implementation() const
{
	return true;
}

ULocalPlayer* UCSLocalPlayerSubsystem::K2_GetLocalPlayer() const
{
	return GetLocalPlayer();
}
