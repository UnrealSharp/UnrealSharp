#include "FCSManagerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void UFCSManagerExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FindManagedObject)
	EXPORT_FUNCTION(GetCurrentWorldContext)
	EXPORT_FUNCTION(GetCurrentWorldPtr)
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

void* UFCSManagerExporter::GetCurrentWorldPtr()
{
	UObject* WorldContext = UCSManager::Get().GetCurrentWorldContext();
	return GEngine->GetWorldFromContextObject(WorldContext, EGetWorldErrorMode::ReturnNull);
}

void UFCSManagerExporter::RegisterDynamicLogCategory(FName CategoryName, ELogVerbosity::Type Verbosity)
{
	UCSManager::Get().RegisterDynamicLogCategory(CategoryName, Verbosity);
}
