#include "CSManager.h"

DECLARE_UNREALSHARP_EXPORTER(FCSManagerExporter)
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
		UObject* WorldContext = UCSManager::Get().GetCurrentWorldContext();
		return GEngine->GetWorldFromContextObject(WorldContext, EGetWorldErrorMode::ReturnNull);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(FindManagedObject)
	EXPORT_UNREALSHARP_FUNCTION(FindOrCreateManagedInterfaceWrapper)
	EXPORT_UNREALSHARP_FUNCTION(GetCurrentWorldContext)
	EXPORT_UNREALSHARP_FUNCTION(GetCurrentWorldPtr)
}
