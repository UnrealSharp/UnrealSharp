#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FMapPropertyExporter)
{
	void* GetKey(FMapProperty* MapProperty)
	{
		return MapProperty->KeyProp;
	}

	void* GetValue(FMapProperty* MapProperty)
	{
		return MapProperty->ValueProp;
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetKey)
	EXPORT_UNREALSHARP_FUNCTION(GetValue)
}
