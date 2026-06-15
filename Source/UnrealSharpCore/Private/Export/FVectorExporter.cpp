#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FVectorExporter)
{
	FVector FromRotator(FRotator Rotator)
	{
		return Rotator.Vector();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(FromRotator)
}
