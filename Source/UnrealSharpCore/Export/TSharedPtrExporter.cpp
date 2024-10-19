#include "TSharedPtrExporter.h"

void UTSharedPtrExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(AddSharedReference)
	EXPORT_FUNCTION(ReleaseSharedReference)
}

void UTSharedPtrExporter::AddSharedReference(SharedPointerInternals::TReferenceControllerBase<ESPMode::ThreadSafe>* ReferenceController)
{
	if (!ReferenceController)
	{
		return;
	}
	
	ReferenceController->AddSharedReference();
}

void UTSharedPtrExporter::ReleaseSharedReference(SharedPointerInternals::TReferenceControllerBase<ESPMode::ThreadSafe>* ReferenceController)
{
	if (!ReferenceController)
	{
		return;
	}

	ReferenceController->ReleaseSharedReference();
}
