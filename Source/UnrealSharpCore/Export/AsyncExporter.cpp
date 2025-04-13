#include "AsyncExporter.h"
#include "UnrealSharpCore/CSManager.h"
#include "CSManagedCallbacksCache.h"

void UAsyncExporter::RunOnThread(UObject* WorldContextObject, ENamedThreads::Type Thread, GCHandleIntPtr DelegateHandle)
{
	AsyncTask(Thread, [=]()
	{
		UCSManager& Manager = UCSManager::Get();
		Manager.SetCurrentWorldContext(WorldContextObject);

		FGCHandle GCHandle(DelegateHandle);
		FCSManagedCallbacks::ManagedCallbacks.InvokeDelegate(DelegateHandle);
		GCHandle.Dispose();
	});
}

int UAsyncExporter::GetCurrentNamedThread()
{
	return FTaskGraphInterface::Get().GetCurrentThreadIfKnown();
}