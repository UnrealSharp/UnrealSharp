#include "Export/TObjectPtrExporter.h"

void UTObjectPtrExporter::SetTObjectPtrPropertyValue(TObjectPtr<UObject>* Object, UObject* NewValue)
{
	*Object = NewValue;
}
