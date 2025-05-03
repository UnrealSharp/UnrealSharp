#include "AsyncExporter.h"
#include "CSManagedDelegate.h"

void UAsyncExporter::RunOnThread(UObject* WorldContextObject, ENamedThreads::Type Thread, FGCHandleIntPtr DelegateHandle)
{
	TWeakObjectPtr<UObject> WeakWorldContextObject(WorldContextObject);
	
	AsyncTask(Thread, [WeakWorldContextObject, DelegateHandle]()
	{
		FCSManagedDelegate ManagedDelegate = FGCHandle(DelegateHandle, GCHandleType::StrongHandle);
		
		if (!WeakWorldContextObject.IsValid())
		{
			ManagedDelegate.Dispose();
			return;
		}

		UObject* WorldContextObject = WeakWorldContextObject.Get();
		ManagedDelegate.Invoke(WorldContextObject);
	});
}

int UAsyncExporter::GetCurrentNamedThread()
{
	return FTaskGraphInterface::Get().GetCurrentThreadIfKnown();
}