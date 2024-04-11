#include "FStringExporter.h"

void UFStringExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(MarshalToNativeString);
	EXPORT_FUNCTION(DisposeString);
}

void UFStringExporter::MarshalToNativeString(FString* String, TCHAR* ManagedString)
{
	if (String == nullptr)
	{
		return;
	}

	*String = FString(ManagedString);
}

void UFStringExporter::DisposeString(FString* String)
{
	if (String == nullptr)
	{
		return;
	}

	String->~FString();
}
