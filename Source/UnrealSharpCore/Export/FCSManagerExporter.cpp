#include "FCSManagerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void UFCSManagerExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FindManagedObject)
	EXPORT_FUNCTION(GetCurrentWorldContext)
}

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object).GetIntPtr();
}

void* UFCSManagerExporter::GetCurrentWorldContext()
{
	return FindManagedObject(UCSManager::Get().GetCurrentWorldContext());
}
