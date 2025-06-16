#include "CSManagedDelegate.h"

#include "CSManager.h"

void FCSManagedDelegate::Invoke(UObject* WorldContextObject, bool bDispose)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSManagedDelegate::Invoke);

	if (CallbackHandle.IsNull())
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "FCSManagedDelegate::Invoke: CallbackHandle is null");
		return;
	}

	if (IsValid(WorldContextObject))
	{
		UCSManager::Get().SetCurrentWorldContext(WorldContextObject);
	}

	FCSManagedCallbacks::ManagedCallbacks.InvokeDelegate(CallbackHandle.GetHandle());

	if (bDispose)
	{
		Dispose();
	}
}
