#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSUnrealSharpUtilitiesSettings.generated.h"

UCLASS(config = EditorPerProjectUserSettings, meta = (DisplayName = "UnrealSharp Proc Helper Settings"))
class UCSUnrealSharpUtilitiesSettings : public UDeveloperSettings
{
	GENERATED_BODY()
public:
	
	UCSUnrealSharpUtilitiesSettings()
	{
		CategoryName = "Plugins";
	}
	
	/**
	* Whether to show build warnings in the build error dialog.
	* Only affects the full dotnet build (editor startup / manual rebuild), not the incremental hot reload compiler.
	*/
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Build Output")
	bool bShowBuildWarnings = false;
};
