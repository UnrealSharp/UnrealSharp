#include "CSManager.h"

DECLARE_UNREALSHARP_BINDER(Bind_ULocalPlayer)
{
	void* GetLocalPlayerSubsystem(UClass* SubsystemClass, APlayerController* PlayerController)
	{
		if (!IsValid(PlayerController) || !IsValid(SubsystemClass))
		{
			return nullptr;
		}

		ULocalPlayerSubsystem* LocalPlayerSubsystem = PlayerController->GetLocalPlayer()->GetSubsystemBase(SubsystemClass);
		return UCSManager::Get().FindManagedObject(LocalPlayerSubsystem);
	}
	
	BIND_UNREALSHARP_FUNCTION(GetLocalPlayerSubsystem)
}
