#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSUnrealSharpEditorSettings.generated.h"

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

UCLASS(config = EditorPerProjectUserSettings, meta = (DisplayName = "UnrealSharp Editor Settings"))
class UCSUnrealSharpEditorSettings : public UDeveloperSettings
{
	GENERATED_BODY()
public:
	UCSUnrealSharpEditorSettings();
	
	// Whether Hot Reload should automatically start on script save, gaining Editor focus, or not at all.
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Hot Reload")
	TEnumAsByte<EAutomaticHotReloadMethod> AutomaticHotReloading = OnScriptSave;

	// Should we suffix generated types' DisplayName with "TypeName (C#)"?
	// Needs restart to take effect.
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Type Generation")
	bool bSuffixGeneratedTypes = false;
	
	// The maximum allowed character length for C# .csproj names
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Projects")
	int32 MaxProjectNameLength = 32;
};
