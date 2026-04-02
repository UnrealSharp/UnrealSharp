#include "Extensions/Libraries/CSPlayerControllerExtensions.h"

ULocalPlayer* UCSPlayerControllerExtensions::GetLocalPlayer(APlayerController* PlayerController)
{
	if (!IsValid(PlayerController))
	{
		return nullptr;
	}

	return PlayerController->GetLocalPlayer();
}
