#include "FCSManagerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void UFCSManagerExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FindManagedObject)
}

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object).GetIntPtr();
}
