#include "CSAsyncActionBase.h"
#include "CSBindsRegistry.h"
#include "UnrealSharpAsync.h"

DECLARE_UNREALSHARP_BINDER(Bind_UCSAsyncBase)
{
	void InitializeAsyncObject(UCSAsyncActionBase* AsyncAction, FGCHandleIntPtr Callback)
	{
		if (!IsValid(AsyncAction))
		{
			UE_LOG(LogUnrealSharpAsync, Warning, TEXT("UUCSAsyncBaseExporter::InitializeAsyncObject: AsyncAction is null"));
			return;
		}
	
		AsyncAction->InitializeManagedCallback(Callback);
	}

	BIND_UNREALSHARP_FUNCTION(InitializeAsyncObject)
}
