#include "CSCancellableAsyncAction.h"

void UCSCancellableAsyncAction::Activate()
{
	ReceiveActivate();
}

void UCSCancellableAsyncAction::Cancel()
{
	if (HasAnyFlags(RF_ClassDefaultObject | RF_PendingKill | RF_BeginDestroyed))
	{
		return;
	}
	ReceiveCancel();
}
