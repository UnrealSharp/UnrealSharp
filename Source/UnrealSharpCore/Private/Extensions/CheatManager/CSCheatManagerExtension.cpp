#include "Extensions/CheatManager/CSCheatManagerExtension.h"

UCSCheatManagerExtension::UCSCheatManagerExtension()
{
	if (HasAnyFlags(RF_ClassDefaultObject) && GetClass() != StaticClass())
	{
		UCheatManager::RegisterForOnCheatManagerCreated(FOnCheatManagerCreated::FDelegate::CreateWeakLambda(this,
			[this](UCheatManager* CheatManager)
			{
				UClass* ThisClass = GetClass();
				CheatManager->AddCheatManagerExtension(NewObject<UCheatManagerExtension>(CheatManager, ThisClass));
			}));

	}
}
