#include "FCSManagerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object).GetPointer();
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
