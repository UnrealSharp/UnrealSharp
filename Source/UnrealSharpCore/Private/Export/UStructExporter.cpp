#include "Export/UStructExporter.h"

void UUStructExporter::InitializeStruct(UStruct* Struct, void* Data)
{
	check(Struct && Data);
	Struct->InitializeStruct(Data);
}
