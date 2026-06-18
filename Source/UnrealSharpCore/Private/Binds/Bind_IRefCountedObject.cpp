#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_IRefCountedObject)
{
	void AddRef(const IRefCountedObject* Object)
	{
		if (!Object)
		{
			return;

		}
		
		Object->AddRef();
	}

	void Release(const IRefCountedObject* Object)
	{
		if (!Object)
		{
			return;
		}
		
		Object->Release();
	}
	
	BIND_UNREALSHARP_FUNCTION(AddRef)
	BIND_UNREALSHARP_FUNCTION(Release)
}