#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FMsg)
{
	void Log(const UTF16CHAR* ManagedCategoryName, ELogVerbosity::Type Verbosity, const UTF16CHAR* ManagedMessage)
	{
		FString Message = FString(ManagedMessage);
		FName CategoryName = FName(ManagedCategoryName);
	
		FMsg::Logf(nullptr, 0, CategoryName, Verbosity, TEXT("%s"), *Message);
	}
	
	BIND_UNREALSHARP_FUNCTION(Log)
}
