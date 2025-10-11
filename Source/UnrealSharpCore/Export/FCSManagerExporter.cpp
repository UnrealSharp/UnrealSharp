#include "FCSManagerExporter.h"
#include "UnrealSharpCore/CSManager.h"

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object);
}

void* UFCSManagerExporter::FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* NativeClass)
{
	return UCSManager::Get().FindOrCreateManagedInterfaceWrapper(Object, NativeClass);
}

void* UFCSManagerExporter::GetCurrentWorldContext()
{
	void* WorldContext = UCSManager::Get().GetCurrentWorldContext();
	return WorldContext;
}

void* UFCSManagerExporter::GetCurrentWorldPtr()
{
	UObject* WorldContext = UCSManager::Get().GetCurrentWorldContext();
	return GEngine->GetWorldFromContextObject(WorldContext, EGetWorldErrorMode::ReturnNull);
}
