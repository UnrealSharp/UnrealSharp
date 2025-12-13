#include "Export/TSharedPtrExporter.h"

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
