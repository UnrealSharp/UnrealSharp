#include "FMsgExporter.h"

void UFMsgExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(Log);
}

void UFMsgExporter::Log(FName CategoryName, ELogVerbosity::Type Verbosity, const UTF16CHAR* Message)
{
	FString MessageStr = FString(Message);
	FMsg::Logf(nullptr, 0, CategoryName, Verbosity, TEXT("%s"), *MessageStr);
}
