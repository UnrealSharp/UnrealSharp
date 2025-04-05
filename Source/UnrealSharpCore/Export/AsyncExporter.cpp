#include "AsyncExporter.h"
#include "UnrealSharpCore/CSManager.h"
#include "CSManagedCallbacksCache.h"

void UAsyncExporter::RunOnThread(UObject* WorldContextObject, ENamedThreads::Type Thread, GCHandleIntPtr DelegateHandle)
{
	TWeakObjectPtr<UObject> WeakWorldContextObject(WorldContextObject);
	
	AsyncTask(Thread, [WeakWorldContextObject, DelegateHandle]()
	{
		FGCHandle Handle(DelegateHandle);
		
		if (!WeakWorldContextObject.IsValid())
		{
			return;
		}

		UObject* WorldContextObject = WeakWorldContextObject.Get();
		UCSManager::Get().SetCurrentWorldContext(WorldContextObject);
		
		FCSManagedCallbacks::ManagedCallbacks.InvokeDelegate(DelegateHandle);
		Handle.Dispose();
	});
}

int UAsyncExporter::GetCurrentNamedThread()
{
	return FTaskGraphInterface::Get().GetCurrentThreadIfKnown();
}