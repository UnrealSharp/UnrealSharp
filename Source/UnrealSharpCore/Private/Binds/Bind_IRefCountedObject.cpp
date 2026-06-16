#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_IRefCountedObject)
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
	
	BIND_UNREALSHARP_FUNCTION(GetRefCount)
	BIND_UNREALSHARP_FUNCTION(AddRef)
	BIND_UNREALSHARP_FUNCTION(Release)
}
