#include "Export/FMatrixExporter.h"

void UFMatrixExporter::FromRotator(FMatrix* Matrix, const FRotator Rotator)
{
	*Matrix = Rotator.Quaternion().ToMatrix();
}
