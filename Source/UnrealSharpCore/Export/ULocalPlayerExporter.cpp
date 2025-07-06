#include "ULocalPlayerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void* UULocalPlayerExporter::GetLocalPlayerSubsystem(UClass* SubsystemClass, APlayerController* PlayerController)
{
	if (!IsValid(PlayerController) || !IsValid(SubsystemClass))
	{
		return nullptr;
	}

	ULocalPlayerSubsystem* LocalPlayerSubsystem = PlayerController->GetLocalPlayer()->GetSubsystemBase(SubsystemClass);
	return UCSManager::Get().FindManagedObject(LocalPlayerSubsystem);
}
