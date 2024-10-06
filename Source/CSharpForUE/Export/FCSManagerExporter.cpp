#include "FCSManagerExporter.h"
#include "CSharpForUE/CSManager.h"

void UFCSManagerExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FindManagedObject)
}

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object).GetIntPtr();
}
