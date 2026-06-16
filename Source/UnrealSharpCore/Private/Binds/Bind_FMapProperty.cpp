#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FMapProperty)
{
	void* GetKey(FMapProperty* MapProperty)
	{
		return MapProperty->KeyProp;
	}

	void* GetValue(FMapProperty* MapProperty)
	{
		return MapProperty->ValueProp;
	}
	
	BIND_UNREALSHARP_FUNCTION(GetKey)
	BIND_UNREALSHARP_FUNCTION(GetValue)
}
