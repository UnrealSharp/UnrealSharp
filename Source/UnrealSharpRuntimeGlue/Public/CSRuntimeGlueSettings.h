#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSRuntimeGlueSettings.generated.h"

class UCSGlueGenerator;

UCLASS(NotBlueprintable, Config = "Editor", DefaultConfig, DisplayName = "UnrealSharp Runtime Glue Settings")
class UCSRuntimeGlueSettings : public UDeveloperSettings
{
	GENERATED_BODY()
public:
	UCSRuntimeGlueSettings();
	
	UPROPERTY(Config, EditAnywhere)
	TArray<TSoftClassPtr<UCSGlueGenerator>> Generators;
};
