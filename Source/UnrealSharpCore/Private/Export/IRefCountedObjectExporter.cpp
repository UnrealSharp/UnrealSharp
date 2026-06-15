#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(IRefCountedObjectExporter)
{
	uint32 GetRefCount(const IRefCountedObject* Object)
	{
		if (!Object || Object->GetRefCount() == 0)
		{
			return 0;
		}
	
		return Object->GetRefCount();
	}

	uint32 AddRef(const IRefCountedObject* Object)
	{
		if (!Object || Object->GetRefCount() == 0)
		{
			return 0;
		}
	
		(void)Object->AddRef();
		return Object->GetRefCount();
	}

	uint32 Release(const IRefCountedObject* Object)
	{
		if (!Object || Object->GetRefCount() == 0)
		{
			return 0;
		}
	
		return Object->Release();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetRefCount)
	EXPORT_UNREALSHARP_FUNCTION(AddRef)
	EXPORT_UNREALSHARP_FUNCTION(Release)
}
