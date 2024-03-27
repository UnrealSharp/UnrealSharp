#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSDeveloperSettings.generated.h"

UCLASS(config = UnrealSharp, meta = (DisplayName = "UnrealSharp Settings"))
class CSHARPFORUE_API UCSDeveloperSettings : public UDeveloperSettings
{
	GENERATED_BODY()

public:

	// Should we exit PIE when an exception is thrown in C#?
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Debugging")
	bool bCrashOnException = true;
	
	// Whether Hot Reload should wait for the Editor to gain focus
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Hot Reload")
	bool bRequireFocusForHotReload = false;
	
};
