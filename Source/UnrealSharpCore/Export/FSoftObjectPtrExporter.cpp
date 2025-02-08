#include "FSoftObjectPtrExporter.h"
#include "UnrealSharpCore/CSManager.h"

void UFSoftObjectPtrExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(LoadSynchronous)
}

void* UFSoftObjectPtrExporter::LoadSynchronous(const TSoftObjectPtr<UObject>* SoftObjectPtr)
{
	if (SoftObjectPtr->IsNull())
	{
		return nullptr;
	}
	
	UObject* Test = SoftObjectPtr->LoadSynchronous();
	
	if (!IsValid(Test))
	{
		return nullptr;
	}
	
	return UCSManager::Get().FindManagedObject(Test).GetPointer();
}
