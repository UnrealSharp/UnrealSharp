#include "CSBindsRegistry.h"
#include "CSManagedDelegate.h"
#include "CSManagedGCHandle.h"

DECLARE_UNREALSHARP_BINDER(Bind_AsyncExporter)
{
	void RunOnThread(TWeakObjectPtr<UObject> WorldContextObject, ENamedThreads::Type Thread, FGCHandleIntPtr DelegateHandle)
	{
		AsyncTask(Thread, [WorldContextObject, DelegateHandle]()
		{
			FCSManagedDelegate ManagedDelegate = FGCHandle(DelegateHandle);
		
			if (!WorldContextObject.IsValid())
			{
				ManagedDelegate.Dispose();
				return;
			}
		
			ManagedDelegate.Invoke(WorldContextObject.Get());
		});
	}

	int GetCurrentNamedThread()
	{
		return FTaskGraphInterface::Get().GetCurrentThreadIfKnown();
	}
	
	BIND_UNREALSHARP_FUNCTION(RunOnThread)
	BIND_UNREALSHARP_FUNCTION(GetCurrentNamedThread)
}
