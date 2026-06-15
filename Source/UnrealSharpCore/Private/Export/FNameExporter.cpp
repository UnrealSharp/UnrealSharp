

#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FNameExporter)
{
	void NameToString(FName Name, FString* OutString)
	{
		Name.ToString(*OutString);
	}

	void StringToName(FName* Name, const UTF16CHAR* String, int32 Length)
	{
		*Name = FName(TStringView(String, Length));
	}

	bool IsValid(FName Name)
	{
		bool bIsValid = Name.IsValid();
		return bIsValid;
	}
	
	EXPORT_UNREALSHARP_FUNCTION(NameToString)
	EXPORT_UNREALSHARP_FUNCTION(StringToName)
	EXPORT_UNREALSHARP_FUNCTION(IsValid)
}
