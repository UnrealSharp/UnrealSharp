#include "Export/FWorldDelegatesExporter.h"

void UFWorldDelegatesExporter::BindOnWorldCleanup(FWorldCleanupEventDelegate Delegate, FDelegateHandle* Handle)
{
	*Handle = FWorldDelegates::OnWorldCleanup.AddLambda(Delegate);
}

void UFWorldDelegatesExporter::UnbindOnWorldCleanup(const FDelegateHandle Handle)
{
	FWorldDelegates::OnWorldCleanup.Remove(Handle);
}
