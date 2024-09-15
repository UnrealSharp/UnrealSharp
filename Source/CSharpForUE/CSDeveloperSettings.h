#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSDeveloperSettings.generated.h"

UENUM()
enum EAutomaticHotReloadMethod : uint8
{
	// Automatically Hot Reloads when script changes are saved
	OnScriptSave,
	// Wait for the Editor to gain focus before Hot Reloading
	OnEditorFocus,
	// Will not Hot Reload automatically
	Off,
};

UCLASS(config = EditorPerProjectUserSettings, meta = (DisplayName = "UnrealSharp Settings"))
class CSHARPFORUE_API UCSDeveloperSettings : public UDeveloperSettings
{
	GENERATED_BODY()

public:

	// Should we exit PIE when an exception is thrown in C#?
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Debugging")
	bool bCrashOnException = true;
	
	// Whether Hot Reload should automatically start on script save, gaining Editor focus, or not at all.
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Hot Reload")
	TEnumAsByte<EAutomaticHotReloadMethod> AutomaticHotReloading = OnScriptSave;
};
