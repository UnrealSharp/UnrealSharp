#include "CSCommonUnrealSharpSettings.h"

#include "CSUnrealSharpSettingsUtilities.h"

FString FCSCommonUnrealSharpSettings::GetScriptDirectoryName()
{
	TSharedPtr<FJsonValue> ScriptDirectoryNameValue = FCSUnrealSharpSettingsUtilities::GetElement(TEXT("ScriptDirectoryName"));
	
	if (!ScriptDirectoryNameValue.IsValid())
	{
		return TEXT("");
	}
	
	return ScriptDirectoryNameValue->AsString();
}
