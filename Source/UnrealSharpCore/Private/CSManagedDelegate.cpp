#include "CSManagedDelegate.h"

#include "CSManager.h"
#include "Engine/World.h"

void FCSManagedDelegate::Invoke(UObject* WorldContextObject, bool bDispose)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSManagedDelegate::Invoke);

	if (CallbackHandle.IsNull())
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "FCSManagedDelegate::Invoke: CallbackHandle is null");
		return;
	}

	// Prefer using World as context since it's more stable
	UObject* WorldContext = nullptr;
	if (IsValid(WorldContextObject))
	{
		UWorld* World = WorldContextObject->GetWorld();
		WorldContext = World ? World : WorldContextObject;
	}

	if (WorldContext)
	{
		UCSManager::Get().SetCurrentWorldContext(WorldContext);
	}

	FCSManagedCallbacks::ManagedCallbacks.InvokeDelegate(CallbackHandle.GetHandle());

	if (bDispose)
	{
		Dispose();
	}
}
