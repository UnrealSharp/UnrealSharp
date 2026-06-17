#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_IRefCountedObject)
{
	struct FCSRefCountType
	{
		static FCSRefCountType Invalid()
		{
#if ENGINE_MINOR_VERSION >= 8
			return FCSRefCountType(FReturnedRefCountValue(0));
#else
			return FCSRefCountType(0);
#endif
		}
       
#if ENGINE_MINOR_VERSION >= 8
		FCSRefCountType(FReturnedRefCountValue InRefCountType) : RefCountType(InRefCountType) {}
		FReturnedRefCountValue RefCountType;
#else
		FCSRefCountType(uint32 InRefCountType) : RefCountType(InRefCountType) {}
		uint32 RefCountType;
#endif
	};

	FCSRefCountType GetRefCount(const IRefCountedObject* Object)
	{
		if (!Object)
		{
			return FCSRefCountType::Invalid();
		}
    
		return FCSRefCountType(Object->GetRefCount());
	}

	FCSRefCountType AddRef(const IRefCountedObject* Object)
	{
		if (!Object)
		{
			return FCSRefCountType::Invalid();
		}
    
		(void)Object->AddRef();
		return FCSRefCountType(Object->GetRefCount());
	}

	FCSRefCountType Release(const IRefCountedObject* Object)
	{
		if (!Object)
		{
			return FCSRefCountType::Invalid();
		}
    
		return FCSRefCountType(Object->Release());
	}
    
	BIND_UNREALSHARP_FUNCTION(GetRefCount)
	BIND_UNREALSHARP_FUNCTION(AddRef)
	BIND_UNREALSHARP_FUNCTION(Release)
}