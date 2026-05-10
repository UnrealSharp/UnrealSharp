#include "Export/TPersistentObjectPtrExporter.h"
#include "CSManager.h"

void UTPersistentObjectPtrExporter::FromObject(TPersistentObjectPtr<FSoftObjectPath>* Path, UObject* InObject)
{
	*Path = InObject;
}

void UTPersistentObjectPtrExporter::FromSoftObjectPath(TPersistentObjectPtr<FSoftObjectPath>* Path, const FSoftObjectPath* SoftObjectPath)
{
	*Path = *SoftObjectPath;
}

void* UTPersistentObjectPtrExporter::Get(TPersistentObjectPtr<FSoftObjectPath>* Path)
{
	UObject* Object = Path->Get();
	return UCSManager::Get().FindManagedObject(Object);
}

void* UTPersistentObjectPtrExporter::GetNativePointer(TPersistentObjectPtr<FSoftObjectPath>* Path)
{
	return Path->Get();
}

void* UTPersistentObjectPtrExporter::GetUniqueID(TPersistentObjectPtr<FSoftObjectPath>* Path)
{
	return &Path->GetUniqueID();
}

bool UTPersistentObjectPtrExporter::Equals(const TPersistentObjectPtr<FSoftObjectPath>* Path, const TPersistentObjectPtr<FSoftObjectPath>* Other)
{
	UObject* Object = Path->Get();
	UObject* OtherObject = Other->Get();
	return Object == OtherObject;
}

int32 UTPersistentObjectPtrExporter::GetHashCode(const TPersistentObjectPtr<FSoftObjectPath>* Path)
{
	return GetTypeHash(Path->GetUniqueID());
}

