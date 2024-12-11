#include "FWorldDelegatesExporter.h"

void UFWorldDelegatesExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(BindOnWorldCleanup)
	EXPORT_FUNCTION(UnbindOnWorldCleanup)
}

void UFWorldDelegatesExporter::BindOnWorldCleanup(FWorldCleanupEventDelegate Delegate, FDelegateHandle& Handle)
{
	Handle = FWorldDelegates::OnWorldCleanup.AddLambda(Delegate);
}

void UFWorldDelegatesExporter::UnbindOnWorldCleanup(const FDelegateHandle Handle)
{
	FWorldDelegates::OnWorldCleanup.Remove(Handle);
}
