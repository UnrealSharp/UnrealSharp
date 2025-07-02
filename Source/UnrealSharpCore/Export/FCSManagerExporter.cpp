#include "FCSManagerExporter.h"

#include "UCoreUObjectExporter.h"
#include "UnrealSharpCore/CSManager.h"

void* UFCSManagerExporter::FindManagedObject(UObject* Object)
{
	return UCSManager::Get().FindManagedObject(Object);
}

void* UFCSManagerExporter::FindOrCreateManagedInterfaceWrapper(UObject* Object, const char* InAssemblyName, const char* InNamespace, const char* InClassName) {
	UClass* NativeClass = UUCoreUObjectExporter::GetNativeClassFromName(InAssemblyName, InNamespace, InClassName);
	return UCSManager::Get().FindOrCreateManagedObjectWrapper(Object, NativeClass);
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
