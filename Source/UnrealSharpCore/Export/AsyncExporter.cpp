#include "AsyncExporter.h"
#include "UnrealSharpCore/CSManager.h"
#include "CSManagedCallbacksCache.h"

void UAsyncExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(RunOnThread)
	EXPORT_FUNCTION(GetCurrentNamedThread)
}

void UAsyncExporter::RunOnThread(UObject* WorldContextObject, ENamedThreads::Type Thread, GCHandleIntPtr DelegateHandle)
{
	AsyncTask(Thread, [=]()
	{
		UCSManager& Manager = UCSManager::Get();
		Manager.SetCurrentWorldContext(WorldContextObject);

		FGCHandle GCHandle(DelegateHandle);
		FCSManagedCallbacks::ManagedCallbacks.InvokeDelegate(DelegateHandle);
		GCHandle.Dispose();

		Manager.SetCurrentWorldContext(nullptr);
	});
}

int UAsyncExporter::GetCurrentNamedThread()
{
	return FTaskGraphInterface::Get().GetCurrentThreadIfKnown();
}