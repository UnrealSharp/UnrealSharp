#include "FStringExporter.h"

void UFStringExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(MarshalToNativeString);
	EXPORT_FUNCTION(DisposeString);
}

void UFStringExporter::MarshalToNativeString(FString* String, TCHAR* ManagedString)
{
	check(String)
	*String = FString(ManagedString);
}

void UFStringExporter::DisposeString(FString* String)
{
	check(String)
	String->Empty();
}
