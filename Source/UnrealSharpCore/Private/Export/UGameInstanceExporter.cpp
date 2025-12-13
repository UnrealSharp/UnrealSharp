#include "Export/UGameInstanceExporter.h"

#include "CSManager.h"

void* UUGameInstanceExporter::GetGameInstanceSubsystem(UClass* SubsystemClass, UObject* WorldContextObject)
{
	if (!IsValid(WorldContextObject))
	{
		return nullptr;
	}
	
	UGameInstanceSubsystem* GameInstanceSubsystem = WorldContextObject->GetWorld()->GetGameInstance()->GetSubsystemBase(SubsystemClass);
	return UCSManager::Get().FindManagedObject(GameInstanceSubsystem);
}
