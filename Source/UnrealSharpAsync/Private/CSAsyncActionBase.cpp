#include "CSAsyncActionBase.h"

void UCSAsyncActionBase::Destroy()
{
	if (UGameInstance* GameInstance = GetWorld()->GetGameInstance())
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

	if (UGameInstance* GameInstance = GetWorld()->GetGameInstance())
	{
		GameInstance->RegisterReferencedObject(this);
	}
}
