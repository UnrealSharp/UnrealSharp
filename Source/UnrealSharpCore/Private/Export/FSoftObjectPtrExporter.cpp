#include "CSBindsManager.h"
#include "CSManager.h"

DECLARE_UNREALSHARP_EXPORTER(FSoftObjectPtrExporter)
{
	void* LoadSynchronous(const TSoftObjectPtr<UObject>* SoftObjectPtr)
	{
		if (SoftObjectPtr->IsNull())
		{
			return nullptr;
		}
	
		UObject* LoadedObject = SoftObjectPtr->LoadSynchronous();
		return UCSManager::Get().FindManagedObject(LoadedObject);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(LoadSynchronous)
}


