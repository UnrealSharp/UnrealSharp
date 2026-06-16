#include "CSManager.h"

DECLARE_UNREALSHARP_BINDER(Bind_TPersistentObjectPtr)
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
	
	BIND_UNREALSHARP_FUNCTION(FromObject)
	BIND_UNREALSHARP_FUNCTION(FromSoftObjectPath)
	BIND_UNREALSHARP_FUNCTION(Get)
	BIND_UNREALSHARP_FUNCTION(GetNativePointer)
	BIND_UNREALSHARP_FUNCTION(GetUniqueID)
	BIND_UNREALSHARP_FUNCTION(Equals)
	BIND_UNREALSHARP_FUNCTION(GetHashCode)
}

