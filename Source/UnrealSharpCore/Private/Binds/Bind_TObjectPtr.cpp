#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_TObjectPtr)
{
	void SetTObjectPtrPropertyValue(TObjectPtr<UObject>* Object, UObject* NewValue)
	{
		*Object = NewValue;
	}
	
	BIND_UNREALSHARP_FUNCTION(SetTObjectPtrPropertyValue)
}
