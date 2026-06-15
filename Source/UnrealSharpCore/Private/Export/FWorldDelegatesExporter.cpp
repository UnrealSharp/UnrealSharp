#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FWorldDelegatesExporter)
{
	using FWorldCleanupEventDelegate = void(*)(UWorld*, bool, bool);
	
	void BindOnWorldCleanup(FWorldCleanupEventDelegate Delegate, FDelegateHandle* Handle)
	{
		*Handle = FWorldDelegates::OnWorldCleanup.AddLambda(Delegate);
	}

	void UnbindOnWorldCleanup(const FDelegateHandle Handle)
	{
		FWorldDelegates::OnWorldCleanup.Remove(Handle);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(BindOnWorldCleanup)
	EXPORT_UNREALSHARP_FUNCTION(UnbindOnWorldCleanup)
}
