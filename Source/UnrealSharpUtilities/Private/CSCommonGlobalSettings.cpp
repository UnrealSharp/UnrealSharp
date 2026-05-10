#include "CSCommonGlobalSettings.h"

#include "CSGlobalSettingsUtilities.h"

FString UnrealSharp::GlobalSettings::Common::GetScriptDirectoryName()
{
	TSharedPtr<FJsonValue> ScriptDirectoryNameValue = GetElement(TEXT("ScriptDirectoryName"));
	
	if (!ScriptDirectoryNameValue.IsValid())
	{
		return TEXT("");
	}
	
	return ScriptDirectoryNameValue->AsString();
}
