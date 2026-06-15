#include "CSManager.h"

DECLARE_UNREALSHARP_EXPORTER(FWeakObjectPtrExporter)
{
	void SetObject(TWeakObjectPtr<UObject>& WeakObject, UObject* Object)
	{
		WeakObject = Object;
	}

	void* GetObject(TWeakObjectPtr<UObject> WeakObjectPtr)
	{
		if (!WeakObjectPtr.IsValid())
		{
			return nullptr;
		}

		UObject* Object = WeakObjectPtr.Get();
		return UCSManager::Get().FindManagedObject(Object);
	}

	bool IsValid(TWeakObjectPtr<UObject> WeakObjectPtr)
	{
		return WeakObjectPtr.IsValid();
	}

	bool IsStale(TWeakObjectPtr<UObject> WeakObjectPtr)
	{
		return WeakObjectPtr.IsStale();
	}

	bool NativeEquals(TWeakObjectPtr<UObject> A, TWeakObjectPtr<UObject> B)
	{
		return A == B;
	}
	
	EXPORT_UNREALSHARP_FUNCTION(SetObject)
	EXPORT_UNREALSHARP_FUNCTION(GetObject)
	EXPORT_UNREALSHARP_FUNCTION(IsValid)
	EXPORT_UNREALSHARP_FUNCTION(IsStale)
	EXPORT_UNREALSHARP_FUNCTION(NativeEquals)
}


