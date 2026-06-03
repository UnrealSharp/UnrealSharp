#include "Export/FCSManagedCallbacksExporter.h"
#include "CSManagedCallbacksCache.h"

FCSManagedCallbacks* UFCSManagedCallbacksExporter::GetManagedCallbacks()
{
	return &::GetManagedCallbacks();
}
