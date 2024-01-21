#include "FSoftObjectPtrExporter.h"
#include "CSharpForUE/CSManager.h"

void UFSoftObjectPtrExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(LoadSynchronous)
}

void* UFSoftObjectPtrExporter::LoadSynchronous(const TSoftObjectPtr<UObject>& SoftObjectPtr)
{
	if (SoftObjectPtr.IsNull())
	{
		return nullptr;
	}
	
	UObject* Test = SoftObjectPtr.Get();
	return FCSManager::Get().FindManagedObject(Test).GetIntPtr();
}
