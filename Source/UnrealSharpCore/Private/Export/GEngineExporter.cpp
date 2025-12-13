#include "Export/GEngineExporter.h"
#include "CSManager.h"
#include "Engine/Engine.h"

void* UGEngineExporter::GetEngineSubsystem(UClass* SubsystemClass)
{
	UEngineSubsystem* EngineSubsystem = GEngine->GetEngineSubsystemBase(SubsystemClass);
	return UCSManager::Get().FindManagedObject(EngineSubsystem);
}
