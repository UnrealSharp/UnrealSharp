#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FName)
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
	
	BIND_UNREALSHARP_FUNCTION(NameToString)
	BIND_UNREALSHARP_FUNCTION(StringToName)
	BIND_UNREALSHARP_FUNCTION(IsValid)
}
