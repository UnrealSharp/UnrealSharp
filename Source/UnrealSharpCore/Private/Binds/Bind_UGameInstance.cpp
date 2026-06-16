#include "CSManager.h"

DECLARE_UNREALSHARP_BINDER(Bind_UGameInstance)
{
	void* GetGameInstanceSubsystem(UClass* SubsystemClass, UObject* WorldContextObject)
	{
		if (!IsValid(WorldContextObject))
		{
			return nullptr;
		}
	
		UGameInstanceSubsystem* GameInstanceSubsystem = WorldContextObject->GetWorld()->GetGameInstance()->GetSubsystemBase(SubsystemClass);
		return UCSManager::Get().FindManagedObject(GameInstanceSubsystem);
	}
	
	BIND_UNREALSHARP_FUNCTION(GetGameInstanceSubsystem)
}
