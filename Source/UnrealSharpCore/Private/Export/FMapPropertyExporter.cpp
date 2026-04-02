#include "Export/FMapPropertyExporter.h"

void* UFMapPropertyExporter::GetKey(FMapProperty* MapProperty)
{
	return MapProperty->KeyProp;
}

void* UFMapPropertyExporter::GetValue(FMapProperty* MapProperty)
{
	return MapProperty->ValueProp;
}
