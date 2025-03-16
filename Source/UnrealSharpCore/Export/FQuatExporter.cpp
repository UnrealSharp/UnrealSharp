#include "FQuatExporter.h"

void UFQuatExporter::ToQuaternion(FQuat& Quaternion, const FRotator& Rotator)
{
	Quaternion = Rotator.Quaternion();
}

void UFQuatExporter::ToRotator(FRotator& Rotator, const FQuat& Quaternion)
{
	Rotator = Quaternion.Rotator();
}

