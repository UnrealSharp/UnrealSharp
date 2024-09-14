#include "FQuatExporter.h"

void UFQuatExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(ToQuaternion)
	EXPORT_FUNCTION(ToRotator)
}

void UFQuatExporter::ToQuaternion(FQuat& Quaternion, const FRotator& Rotator)
{
	Quaternion = Rotator.Quaternion();
}

void UFQuatExporter::ToRotator(FRotator& Rotator, const FQuat& Quaternion)
{
	Rotator = Quaternion.Rotator();
}

