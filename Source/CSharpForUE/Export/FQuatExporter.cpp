#include "FQuatExporter.h"

void UFQuatExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(ToQuaternion)
}

void UFQuatExporter::ToQuaternion(FQuat& Quaternion, const FRotator& Rotator)
{
	Quaternion = Rotator.Quaternion();
}

