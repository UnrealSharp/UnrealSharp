#include "CSManager.h"
#include "Engine/Engine.h"

DECLARE_UNREALSHARP_BINDER(Bind_GEngine)
{
	void* GetEngineSubsystem(UClass* SubsystemClass)
	{
		UEngineSubsystem* EngineSubsystem = GEngine->GetEngineSubsystemBase(SubsystemClass);
		return UCSManager::Get().FindManagedObject(EngineSubsystem);
	}
	
	BIND_UNREALSHARP_FUNCTION(GetEngineSubsystem)
}
