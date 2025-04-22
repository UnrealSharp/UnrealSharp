#include "CSUnrealSharpEditorSettings.h"

FString UCSUnrealSharpEditorSettings::GetBuildConfigurationString() const
{
	return StaticEnum<ECSBuildConfiguration>()->GetNameStringByValue(BuildConfiguration);
}

FString UCSUnrealSharpEditorSettings::GetLogVerbosityString() const
{
	return StaticEnum<ECSLoggerVerbosity>()->GetNameStringByValue(LogVerbosity);
}
