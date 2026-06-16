#include "CSBindsRegistry.h"
#include "CSManager.h"

DECLARE_UNREALSHARP_BINDER(Bind_FSoftObjectPtr)
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
	
	BIND_UNREALSHARP_FUNCTION(LoadSynchronous)
}


