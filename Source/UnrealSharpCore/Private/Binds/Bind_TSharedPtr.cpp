#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_TSharedPtr)
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
	
	BIND_UNREALSHARP_FUNCTION(AddSharedReference)
	BIND_UNREALSHARP_FUNCTION(ReleaseSharedReference)
	
}
