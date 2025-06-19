#include "UGameInstanceExporter.h"
#include "UnrealSharpCore/CSManager.h"

void* UUGameInstanceExporter::GetGameInstanceSubsystem(UClass* SubsystemClass, UObject* WorldContextObject)
{
	if (!IsValid(WorldContextObject))
	{
		return nullptr;
	}
	
	UGameInstanceSubsystem* GameInstanceSubsystem = WorldContextObject->GetWorld()->GetGameInstance()->GetSubsystemBase(SubsystemClass);
	return UCSManager::Get().FindManagedObject(GameInstanceSubsystem);
}
