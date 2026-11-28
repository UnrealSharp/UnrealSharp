#include "Extensions/Libraries/CSPlayerControllerExtensions.h"
#include "GameFramework/PlayerController.h"

ULocalPlayer* UCSPlayerControllerExtensions::GetLocalPlayer(APlayerController* PlayerController)
{
	if (!IsValid(PlayerController))
	{
		return nullptr;
	}

	return PlayerController->GetLocalPlayer();
}
