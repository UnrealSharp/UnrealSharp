#include "AsyncExporter.h"
#include "CSManagedDelegate.h"

void UAsyncExporter::RunOnThread(TWeakObjectPtr<UObject> WorldContextObject, ENamedThreads::Type Thread, FGCHandleIntPtr DelegateHandle)
{
	AsyncTask(Thread, [WorldContextObject, DelegateHandle]()
	{
		FCSManagedDelegate ManagedDelegate = FGCHandle(DelegateHandle, GCHandleType::StrongHandle);
		
		if (!WorldContextObject.IsValid())
		{
			ManagedDelegate.Dispose();
			return;
		}
		
		ManagedDelegate.Invoke(WorldContextObject.Get());
	});
}

int UAsyncExporter::GetCurrentNamedThread()
{
	return FTaskGraphInterface::Get().GetCurrentThreadIfKnown();
}