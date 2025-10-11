#include "CSCancellableAsyncAction.h"

void UCSCancellableAsyncAction::Activate()
{
	Super::Activate();
	ReceiveActivate();
}

void UCSCancellableAsyncAction::Cancel()
{
	if (HasAnyFlags(RF_ClassDefaultObject | RF_ArchetypeObject) || !IsValid(this))
	{
		return;
	}

	ReceiveCancel();
	Super::Cancel();
}
