#pragma once

#include "CoreMinimal.h"
#include "Subsystems/EngineSubsystem.h"
#include "CSManagedSubsystemManager.generated.h"

UCLASS()
class UCSManagedSubsystemManager : public UObject
{
	GENERATED_BODY()
public:
	static UCSManagedSubsystemManager* Get()
	{
		if (!Instance)
		{
			check(IsInGameThread());
			Instance = NewObject<UCSManagedSubsystemManager>(GetTransientPackage(), NAME_None, RF_Public | RF_MarkAsRootSet);
			FModuleManager::Get().OnModulesChanged().AddUObject(Instance, &UCSManagedSubsystemManager::OnModulesChanged);
		}
		
		return Instance;
	}
	
	void ActivateSubsystemClass(TSubclassOf<USubsystem> SubsystemClass);

private:
	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	void InitializeSubsystems();
	
	UPROPERTY(Transient)
	TArray<TSubclassOf<USubsystem>> PendingSubsystems;
	
	static UCSManagedSubsystemManager* Instance;
};
