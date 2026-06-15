#include "CSManager.h"

DECLARE_UNREALSHARP_EXPORTER(TPersistentObjectPtrExporter)
{
	void FromObject(TPersistentObjectPtr<FSoftObjectPath>* Path, UObject* InObject)
	{
		*Path = InObject;
	}

	void FromSoftObjectPath(TPersistentObjectPtr<FSoftObjectPath>* Path, const FSoftObjectPath* SoftObjectPath)
	{
		*Path = *SoftObjectPath;
	}

	void* Get(TPersistentObjectPtr<FSoftObjectPath>* Path)
	{
		UObject* Object = Path->Get();
		return UCSManager::Get().FindManagedObject(Object);
	}

	void* GetNativePointer(TPersistentObjectPtr<FSoftObjectPath>* Path)
	{
		return Path->Get();
	}

	void* GetUniqueID(TPersistentObjectPtr<FSoftObjectPath>* Path)
	{
		return &Path->GetUniqueID();
	}

	bool Equals(const TPersistentObjectPtr<FSoftObjectPath>* Path, const TPersistentObjectPtr<FSoftObjectPath>* Other)
	{
		UObject* Object = Path->Get();
		UObject* OtherObject = Other->Get();
		return Object == OtherObject;
	}

	int32 GetHashCode(const TPersistentObjectPtr<FSoftObjectPath>* Path)
	{
		return GetTypeHash(Path->GetUniqueID());
	}
	
	EXPORT_UNREALSHARP_FUNCTION(FromObject)
	EXPORT_UNREALSHARP_FUNCTION(FromSoftObjectPath)
	EXPORT_UNREALSHARP_FUNCTION(Get)
	EXPORT_UNREALSHARP_FUNCTION(GetNativePointer)
	EXPORT_UNREALSHARP_FUNCTION(GetUniqueID)
	EXPORT_UNREALSHARP_FUNCTION(Equals)
	EXPORT_UNREALSHARP_FUNCTION(GetHashCode)
}

