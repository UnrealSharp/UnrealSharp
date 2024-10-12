#include "CSCancellableAsyncAction.h"

void UCSCancellableAsyncAction::Activate()
{
	ReceiveActivate();
}

void UCSCancellableAsyncAction::Cancel()
{
	if (HasAnyFlags(RF_ClassDefaultObject) || !IsValid(this))
	{
		return;
	}

	ReceiveCancel();
}
