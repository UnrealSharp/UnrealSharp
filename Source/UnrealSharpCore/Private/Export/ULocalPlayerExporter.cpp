#include "CSManager.h"

DECLARE_UNREALSHARP_EXPORTER(ULocalPlayerExporter)
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
	
	EXPORT_UNREALSHARP_FUNCTION(GetLocalPlayerSubsystem)
}
