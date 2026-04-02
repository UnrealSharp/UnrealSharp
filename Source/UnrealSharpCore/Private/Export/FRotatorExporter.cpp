#include "Export/FRotatorExporter.h"

void UFRotatorExporter::FromMatrix(FRotator* Rotator, const FMatrix& Matrix)
{
	*Rotator = Matrix.Rotator();
}


