#include "CSManagedDelegate.h"

#include "CSManager.h"
#include "CSWorldContextScope.h"
void FCSManagedDelegate::Invoke(UObject* WorldContextObject, bool bDispose)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSManagedDelegate::Invoke);

	if (CallbackHandle.IsNull())
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "FCSManagedDelegate::Invoke: CallbackHandle is null");
		return;
	}

	FCSWorldContextScope WorldContextScope(WorldContextObject);

	GetManagedCallbacks().InvokeDelegate(CallbackHandle.GetHandle());

	if (bDispose)
	{
		Dispose();
	}
}
