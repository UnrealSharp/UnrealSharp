#include "FCSManagerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void UFCSManagerExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FindManagedObject)
	EXPORT_FUNCTION(GetCurrentWorldContext)
	EXPORT_FUNCTION(RegisterDynamicLogCategory)
}

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object).GetIntPtr();
}

void* UFCSManagerExporter::GetCurrentWorldContext()
{
	return FindManagedObject(UCSManager::Get().GetCurrentWorldContext());
}

void UFCSManagerExporter::RegisterDynamicLogCategory(FName CategoryName, ELogVerbosity::Type Verbosity)
{
	UCSManager::Get().RegisterDynamicLogCategory(CategoryName, Verbosity);
}
