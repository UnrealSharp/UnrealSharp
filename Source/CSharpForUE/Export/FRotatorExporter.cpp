#include "FRotatorExporter.h"

void UFRotatorExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FromQuat)
	EXPORT_FUNCTION(FromMatrix)
}

void UFRotatorExporter::FromQuat(FRotator& Rotator, const FQuat& Quaternion)
{
	Rotator = Quaternion.Rotator();
}

void UFRotatorExporter::FromMatrix(FRotator& Rotator, const FMatrix& Matrix)
{
	Rotator = Matrix.Rotator();
}


