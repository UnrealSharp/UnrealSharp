#include "FStringExporter.h"

void UFStringExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(MarshalToNativeString);
}

void UFStringExporter::MarshalToNativeString(FString* String, TCHAR* ManagedString)
{
	*String = ManagedString;
}
