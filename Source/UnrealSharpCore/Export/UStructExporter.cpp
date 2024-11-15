#include "UStructExporter.h"

void UUStructExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(InitializeStruct)
}

void UUStructExporter::InitializeStruct(UStruct* Struct, void* Data)
{
	check(Struct && Data);
	Struct->InitializeStruct(Data);
}
