#include "CSManager.h"

DECLARE_UNREALSHARP_EXPORTER(UGameInstanceExporter)
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
	
	EXPORT_UNREALSHARP_FUNCTION(GetGameInstanceSubsystem)
}
