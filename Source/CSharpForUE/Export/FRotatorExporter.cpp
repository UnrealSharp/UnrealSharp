#include "FRotatorExporter.h"

void UFRotatorExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FromMatrix)
}

void UFRotatorExporter::FromMatrix(FRotator& Rotator, const FMatrix& Matrix)
{
	Rotator = Matrix.Rotator();
}


