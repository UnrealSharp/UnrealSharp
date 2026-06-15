#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(TSharedPtrExporter)
{
	void AddSharedReference(SharedPointerInternals::TReferenceControllerBase<ESPMode::ThreadSafe>* ReferenceController)
	{
		if (!ReferenceController)
		{
			return;
		}
	
		ReferenceController->AddSharedReference();
	}

	void ReleaseSharedReference(SharedPointerInternals::TReferenceControllerBase<ESPMode::ThreadSafe>* ReferenceController)
	{
		if (!ReferenceController)
		{
			return;
		}

		ReferenceController->ReleaseSharedReference();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(AddSharedReference)
	EXPORT_UNREALSHARP_FUNCTION(ReleaseSharedReference)
	
}
