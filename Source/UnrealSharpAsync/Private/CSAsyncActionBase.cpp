#include "CSAsyncActionBase.h"

#include "Engine/GameInstance.h"
#include "Engine/World.h"

void UCSAsyncActionBase::Destroy()
{
	UWorld* World = GetWorld();
	if (UGameInstance* GameInstance = World ? World->GetGameInstance() : nullptr)
	{
		GameInstance->UnregisterReferencedObject(this);
	}

	ManagedCallback.Dispose();
	MarkAsGarbage();
}

void UCSAsyncActionBase::InvokeManagedCallback(bool bDispose)
{
	InvokeManagedCallback(this, bDispose);
}

void UCSAsyncActionBase::InvokeManagedCallback(UObject* WorldContextObject, bool bDispose)
{
    ManagedCallback.Invoke(WorldContextObject, bDispose);

    if (bDispose)
    {
        Destroy();
    }
}

void UCSAsyncActionBase::InitializeManagedCallback(FGCHandleIntPtr Callback)
{
	ManagedCallback = FGCHandle(Callback);

	UWorld* World = GetWorld();
	if (UGameInstance* GameInstance = World ? World->GetGameInstance() : nullptr)
	{
		GameInstance->RegisterReferencedObject(this);
	}
}
