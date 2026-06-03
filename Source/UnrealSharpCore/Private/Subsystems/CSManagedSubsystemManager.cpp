// Copyright Ember. All Rights Reserved.


#include "Subsystems/CSManagedSubsystemManager.h"

UCSManagedSubsystemManager* UCSManagedSubsystemManager::Instance = nullptr;

void UCSManagedSubsystemManager::ActivateSubsystemClass(TSubclassOf<USubsystem> SubsystemClass)
{
	PendingSubsystems.AddUnique(SubsystemClass);
	InitializeSubsystems();
}

void UCSManagedSubsystemManager::OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason)
{
	if (InModuleChangeReason != EModuleChangeReason::ModuleLoaded)
	{
		return;
	}
	
	InitializeSubsystems();
}

void UCSManagedSubsystemManager::InitializeSubsystems()
{
	for (int32 i = PendingSubsystems.Num() - 1; i >= 0; --i)
	{
		TSubclassOf<USubsystem> SubsystemClass = PendingSubsystems[i];

		FSubsystemCollectionBase::ActivateExternalSubsystem(SubsystemClass);
		
		TArray<UObject*> FoundSubsystems;
		GetObjectsOfClass(SubsystemClass, FoundSubsystems);
		
		if (FoundSubsystems.IsEmpty())
		{
			continue;
		}
		
		PendingSubsystems.RemoveAt(i);
	}
}
