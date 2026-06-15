#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FRotatorExporter)
{
	void FromMatrix(FRotator* Rotator, const FMatrix& Matrix)
	{
		*Rotator = Matrix.Rotator();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(FromMatrix)
}




