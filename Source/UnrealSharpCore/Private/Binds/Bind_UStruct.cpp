#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_UStruct)
{
	void InitializeStruct(UStruct* Struct, void* Data)
	{
		check(Struct && Data);
		Struct->InitializeStruct(Data);
	}
	
	BIND_UNREALSHARP_FUNCTION(InitializeStruct)
}
