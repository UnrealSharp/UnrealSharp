#include "Export/FSetPropertyExporter.h"

void* UFSetPropertyExporter::GetElement(FSetProperty* Property)
{
	return Property->ElementProp;
}
