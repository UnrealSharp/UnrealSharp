#include "FCSManagerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void UFCSManagerExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FindManagedObject)
	EXPORT_FUNCTION(GetCurrentWorldContext)
	EXPORT_FUNCTION(GetCurrentWorldPtr)
}

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object).GetIntPtr();
}

void* UFCSManagerExporter::GetCurrentWorldContext()
{
	UObject* WorldContext = UCSManager::Get().GetCurrentWorldContext();
	return WorldContext;
}

void* UFCSManagerExporter::GetCurrentWorldPtr()
{
	UObject* WorldContext = UCSManager::Get().GetCurrentWorldContext();
	return GEngine->GetWorldFromContextObject(WorldContext, EGetWorldErrorMode::ReturnNull);
}
