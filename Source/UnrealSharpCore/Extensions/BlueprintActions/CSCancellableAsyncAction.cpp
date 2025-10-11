#include "CSCancellableAsyncAction.h"

void UCSCancellableAsyncAction::Activate()
{
	ReceiveActivate();
	
#if WITH_EDITOR
    FEditorDelegates::PrePIEEnded.AddWeakLambda(this, [this](bool)
    {
        Cancel();
        FEditorDelegates::PrePIEEnded.RemoveAll(this);
    });
#endif
}

void UCSCancellableAsyncAction::Cancel()
{
	if (HasAnyFlags(RF_ClassDefaultObject | RF_ArchetypeObject) || !IsValid(this) || IsUnreachable())
	{
		return;
	}

	ReceiveCancel();
}
