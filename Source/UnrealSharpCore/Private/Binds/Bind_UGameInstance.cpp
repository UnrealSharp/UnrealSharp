#include "CSManager.h"

#include "Engine/GameInstance.h"
#include "Engine/World.h"

DECLARE_UNREALSHARP_BINDER(Bind_UGameInstance)
{
	void* GetGameInstanceSubsystem(UClass* SubsystemClass, UObject* WorldContextObject)
	{
		if (!IsValid(WorldContextObject) || !IsValid(SubsystemClass))
		{
			return nullptr;
		}

		UWorld* World = UCSManager::Get().ResolveWorldContext(WorldContextObject);
		UGameInstance* GameInstance = IsValid(World) ? World->GetGameInstance() : nullptr;
		if (!IsValid(GameInstance))
		{
			return nullptr;
		}

		UGameInstanceSubsystem* GameInstanceSubsystem = GameInstance->GetSubsystemBase(SubsystemClass);
		return UCSManager::Get().FindManagedObject(GameInstanceSubsystem);
	}
	
	BIND_UNREALSHARP_FUNCTION(GetGameInstanceSubsystem)
}
