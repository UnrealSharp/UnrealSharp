#include "FNameExporter.h"

void UFNameExporter::NameToString(FName Name, FString* OutString)
{
	Name.ToString(*OutString);
}

void UFNameExporter::StringToName(FName* Name, const UTF16CHAR* String, int32 Length)
{
	*Name = FName(TStringView(String, Length));
}

bool UFNameExporter::IsValid(FName Name)
{
	bool bIsValid = Name.IsValid();
	return bIsValid;
}
