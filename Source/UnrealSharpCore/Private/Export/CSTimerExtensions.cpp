#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(CSTimerExtensions)
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
	
	EXPORT_UNREALSHARP_FUNCTION(SetTimerForNextTick)
}

