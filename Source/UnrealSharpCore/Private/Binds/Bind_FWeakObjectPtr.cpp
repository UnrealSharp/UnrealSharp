#include "CSManager.h"

DECLARE_UNREALSHARP_BINDER(Bind_FWeakObjectPtr)
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
	
	BIND_UNREALSHARP_FUNCTION(SetObject)
	BIND_UNREALSHARP_FUNCTION(GetObject)
	BIND_UNREALSHARP_FUNCTION(IsValid)
	BIND_UNREALSHARP_FUNCTION(IsStale)
	BIND_UNREALSHARP_FUNCTION(NativeEquals)
}


