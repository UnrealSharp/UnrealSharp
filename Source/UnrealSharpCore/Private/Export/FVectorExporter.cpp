#include "Export/FVectorExporter.h"

FVector UFVectorExporter::FromRotator(const FRotator& Rotator)
{
	return Rotator.Vector();
}
