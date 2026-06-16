#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_TimerExtensions)
{
	using FNextTickEvent = void(*)();
	
	void SetTimerForNextTick(FNextTickEvent NextTickEvent)
	{
#if WITH_EDITOR
		FFunctionGraphTask::CreateAndDispatchWhenReady([NextTickEvent]
		{
			GEditor->GetTimerManager()->SetTimerForNextTick(FTimerDelegate::CreateLambda(NextTickEvent));
		}, TStatId(), nullptr, ENamedThreads::GameThread);
#endif
	}
	
	BIND_UNREALSHARP_FUNCTION(SetTimerForNextTick)
}

