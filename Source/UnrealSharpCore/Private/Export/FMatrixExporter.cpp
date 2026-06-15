#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FMatrixExporter)
{
	void FromRotator(FMatrix* Matrix, const FRotator Rotator)
	{
		*Matrix = Rotator.Quaternion().ToMatrix();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(FromRotator)
}

