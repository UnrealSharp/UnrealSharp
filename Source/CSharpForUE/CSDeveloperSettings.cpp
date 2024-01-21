#include "CSDeveloperSettings.h"

void GetBuildConfiguration(FString& OutBuildConfiguration, EDotNetBuildConfiguration BuildConfiguration)
{
	OutBuildConfiguration = BuildConfiguration == EDotNetBuildConfiguration::Debug ? "Debug" : "Release";
}

void UCSDeveloperSettings::GetBindingsBuildConfiguration(FString& OutBuildConfiguration) const
{
	GetBuildConfiguration(OutBuildConfiguration, BindingsBuildConfiguration);
}

void UCSDeveloperSettings::GetUserBuildConfiguration(FString& OutBuildConfiguration) const
{
	GetBuildConfiguration(OutBuildConfiguration, UserBuildConfiguration);
}
