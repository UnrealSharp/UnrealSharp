#include "CSTimerExtensions.h"

void UCSTimerExtensions::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(SetTimerForNextTick)
}

void UCSTimerExtensions::SetTimerForNextTick(FNextTickEvent NextTickEvent)
{
	FFunctionGraphTask::CreateAndDispatchWhenReady([NextTickEvent]
	{
		GEditor->GetTimerManager()->SetTimerForNextTick(FTimerDelegate::CreateLambda(NextTickEvent));
	}, TStatId(), nullptr, ENamedThreads::GameThread);

}
