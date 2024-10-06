#include "FNameExporter.h"

void UFNameExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(NameToString)
	EXPORT_FUNCTION(StringToName)
	EXPORT_FUNCTION(IsValid)
}

void UFNameExporter::NameToString(FName Name, FString& OutString)
{
	Name.ToString(OutString);
}

void UFNameExporter::StringToName(FName* Name, const UTF16CHAR* String)
{
	*Name = FName(String);
}

bool UFNameExporter::IsValid(FName Name)
{
	return Name.IsValid();
}
