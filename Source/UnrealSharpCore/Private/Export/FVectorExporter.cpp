#include "Export/FVectorExporter.h"

FVector UFVectorExporter::FromRotator(FRotator Rotator)
{
	return Rotator.Vector();
}
