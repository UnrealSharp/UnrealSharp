#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSDeveloperSettings.generated.h"

UENUM()
enum EDotNetBuildConfiguration : uint8
{
	Debug,
	Release
};

UCLASS(config = UnrealSharp, meta = (DisplayName = "UnrealSharp Settings"))
class CSHARPFORUE_API UCSDeveloperSettings : public UDeveloperSettings
{
	GENERATED_BODY()

public:

	void GetBindingsBuildConfiguration(FString& OutBuildConfiguration) const;
	void GetUserBuildConfiguration(FString& OutBuildConfiguration) const;

	// The build configuration to use when building the bindings for UnrealSharp.
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Build Configuration", meta = (ConfigRestartRequired=true))
	TEnumAsByte<EDotNetBuildConfiguration> BindingsBuildConfiguration = EDotNetBuildConfiguration::Debug;

	// The build configuration to use when building the user's assembly.
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Build Configuration")
	TEnumAsByte<EDotNetBuildConfiguration> UserBuildConfiguration = EDotNetBuildConfiguration::Debug;

	// Should we exit PIE when an exception is thrown in C#?
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Debugging")
	bool bCrashOnException = true;
	
	// Whether Hot Reload should wait for the Editor to gain focus
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Hot Reload")
	bool bRequireFocusForHotReload = false;
};
