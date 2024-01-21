#include "TPersistentObjectPtrExporter.h"
#include "CSharpForUE/CSManager.h"

void UTPersistentObjectPtrExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FromObject)
	EXPORT_FUNCTION(Get)
	EXPORT_FUNCTION(GetNativePointer)
}

void UTPersistentObjectPtrExporter::FromObject(TPersistentObjectPtr<FSoftObjectPath>& Path, UObject* InObject)
{
	Path = InObject;
}

void* UTPersistentObjectPtrExporter::Get(TPersistentObjectPtr<FSoftObjectPath>& Path)
{
	UObject* Object = Path.Get();
	return FCSManager::Get().FindManagedObject(Object).GetIntPtr();
}

void* UTPersistentObjectPtrExporter::GetNativePointer(TPersistentObjectPtr<FSoftObjectPath>& Path)
{
	return Path.Get();
}
