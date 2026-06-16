#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FSetProperty)
{
	void* GetElement(FSetProperty* Property)
	{
		return Property->ElementProp;
	}
	
	BIND_UNREALSHARP_FUNCTION(GetElement)
}
