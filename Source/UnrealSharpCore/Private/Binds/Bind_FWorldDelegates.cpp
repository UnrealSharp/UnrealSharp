#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FWorldDelegates)
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
	
	BIND_UNREALSHARP_FUNCTION(BindOnWorldCleanup)
	BIND_UNREALSHARP_FUNCTION(UnbindOnWorldCleanup)
}
