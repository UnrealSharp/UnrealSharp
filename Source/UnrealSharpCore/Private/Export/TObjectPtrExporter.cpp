#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(TObjectPtrExporter)
{
	void SetTObjectPtrPropertyValue(TObjectPtr<UObject>* Object, UObject* NewValue)
	{
		*Object = NewValue;
	}
	
	EXPORT_UNREALSHARP_FUNCTION(SetTObjectPtrPropertyValue)
}
