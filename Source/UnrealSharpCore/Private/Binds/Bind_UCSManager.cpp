#include "CSManager.h"

DECLARE_UNREALSHARP_BINDER(Bind_UCSManager)
{
	void* FindManagedObject(UObject* Object)
	{
		return UCSManager::Get().FindManagedObject(Object);
	}

	void* FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* NativeClass)
	{
		return UCSManager::Get().FindManagedInterfaceWrapper(Object, NativeClass);
	}

	void* GetCurrentWorldContext()
	{
		void* WorldContext = UCSManager::Get().GetCurrentWorldContext();
		return WorldContext;
	}

	void* GetCurrentWorldPtr()
	{
		return UCSManager::Get().GetCurrentWorldContext();
	}

	void PushWorldContext(UObject* WorldContextObject)
	{
		UCSManager::Get().PushWorldContext(WorldContextObject, false);
	}

	void PopWorldContext()
	{
		UCSManager::Get().PopWorldContext();
	}
	
	BIND_UNREALSHARP_FUNCTION(FindManagedObject)
	BIND_UNREALSHARP_FUNCTION(FindOrCreateManagedInterfaceWrapper)
	BIND_UNREALSHARP_FUNCTION(GetCurrentWorldContext)
	BIND_UNREALSHARP_FUNCTION(GetCurrentWorldPtr)
	BIND_UNREALSHARP_FUNCTION(PushWorldContext)
	BIND_UNREALSHARP_FUNCTION(PopWorldContext)
}
