#include "FSoftObjectPtrExporter.h"
#include "UnrealSharpCore/CSManager.h"

void* UFSoftObjectPtrExporter::LoadSynchronous(const TSoftObjectPtr<UObject>* SoftObjectPtr)
{
	if (SoftObjectPtr->IsNull())
	{
		return nullptr;
	}
	
	UObject* LoadedObject = SoftObjectPtr->LoadSynchronous();
	return UCSManager::Get().FindManagedObject(LoadedObject);
}
