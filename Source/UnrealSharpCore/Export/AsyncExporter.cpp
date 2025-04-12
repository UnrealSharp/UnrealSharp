#include "AsyncExporter.h"
#include "UnrealSharpCore/CSManager.h"
#include "CSManagedCallbacksCache.h"

void UAsyncExporter::RunOnThread(UObject* WorldContextObject, ENamedThreads::Type Thread, FGCHandleIntPtr DelegateHandle)
{
	TWeakObjectPtr<UObject> WeakWorldContextObject(WorldContextObject);
	
	AsyncTask(Thread, [WeakWorldContextObject, DelegateHandle]()
	{
		FGCHandle Handle(DelegateHandle, GCHandleType::StrongHandle);
		
		if (!WeakWorldContextObject.IsValid())
		{
			Handle.Dispose();
			return;
		}

		UObject* WorldContextObject = WeakWorldContextObject.Get();
		UCSManager::Get().SetCurrentWorldContext(WorldContextObject);
		
		FCSManagedCallbacks::ManagedCallbacks.InvokeDelegate(Handle.GetHandle());
		Handle.Dispose();
	});
}

int UAsyncExporter::GetCurrentNamedThread()
{
	return FTaskGraphInterface::Get().GetCurrentThreadIfKnown();
}