#include "FCSManagerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object);
}

void* UFCSManagerExporter::GetCurrentWorldContext()
{
	return UCSManager::Get().GetCurrentWorldContext();
}

void* UFCSManagerExporter::GetCurrentWorldPtr()
{
	UObject* WorldContext = UCSManager::Get().GetCurrentWorldContext();
	return GEngine->GetWorldFromContextObject(WorldContext, EGetWorldErrorMode::ReturnNull);
}
