#include "CSManager.h"
#include "Engine/Engine.h"

DECLARE_UNREALSHARP_EXPORTER(GEngineExporter)
{
	void* GetEngineSubsystem(UClass* SubsystemClass)
	{
		UEngineSubsystem* EngineSubsystem = GEngine->GetEngineSubsystemBase(SubsystemClass);
		return UCSManager::Get().FindManagedObject(EngineSubsystem);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetEngineSubsystem)
}
