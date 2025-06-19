#include "GEngineExporter.h"
#include "Engine/Engine.h"
#include "UnrealSharpCore/CSManager.h"

void* UGEngineExporter::GetEngineSubsystem(UClass* SubsystemClass)
{
	UEngineSubsystem* EngineSubsystem = GEngine->GetEngineSubsystemBase(SubsystemClass);
	return UCSManager::Get().FindManagedObject(EngineSubsystem);
}
