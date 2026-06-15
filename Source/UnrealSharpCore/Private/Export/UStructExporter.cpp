#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(UStructExporter)
{
	void InitializeStruct(UStruct* Struct, void* Data)
	{
		check(Struct && Data);
		Struct->InitializeStruct(Data);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(InitializeStruct)
}
