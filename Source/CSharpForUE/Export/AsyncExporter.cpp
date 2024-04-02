#include "AsyncExporter.h"
#include "CSharpForUE/CSManager.h"
#include "CSManagedCallbacksCache.h"
#include "HAL/ThreadManager.h"

void UAsyncExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(RunOnThread)
	EXPORT_FUNCTION(GetCurrentNamedThread)
}

void UAsyncExporter::RunOnThread(ENamedThreads::Type Thread, GCHandleIntPtr DelegateHandle)
{
	AsyncTask(Thread, [=]()
	{
		FGCHandle GCHandle(DelegateHandle);
		FCSManagedCallbacks::ManagedCallbacks.InvokeDelegate(DelegateHandle);
		GCHandle.Dispose();
	});
}

int UAsyncExporter::GetCurrentNamedThread()
{
	return FTaskGraphInterface::Get().GetCurrentThreadIfKnown();
}