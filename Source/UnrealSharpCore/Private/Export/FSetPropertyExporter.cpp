#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FSetPropertyExporter)
{
	void* GetElement(FSetProperty* Property)
	{
		return Property->ElementProp;
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetElement)
}
