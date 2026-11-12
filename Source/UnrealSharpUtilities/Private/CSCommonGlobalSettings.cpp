#include "CSCommonGlobalSettings.h"

#include "CSGlobalSettingsUtilities.h"
#include "Dom/JsonValue.h"

FString UnrealSharp::GlobalSettings::Common::GetScriptDirectoryName()
{
	TSharedPtr<FJsonValue> ScriptDirectoryNameValue = GetElement(TEXT("ScriptDirectoryName"));
	
	if (!ScriptDirectoryNameValue.IsValid())
	{
		return TEXT("");
	}
	
	return ScriptDirectoryNameValue->AsString();
}
