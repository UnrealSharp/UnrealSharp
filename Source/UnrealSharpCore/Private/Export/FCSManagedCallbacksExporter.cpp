#include "Export/FCSManagedCallbacksExporter.h"
#include "CSManagedCallbacksCache.h"

FCSManagedCallbacks::FManagedCallbacks* UFCSManagedCallbacksExporter::GetManagedCallbacks()
{
	return &FCSManagedCallbacks::ManagedCallbacks;
}
