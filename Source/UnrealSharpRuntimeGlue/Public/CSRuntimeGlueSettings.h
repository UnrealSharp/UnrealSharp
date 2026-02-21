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

	// UObject interface
	virtual void PostEditChangeProperty(struct FPropertyChangedEvent& PropertyChangedEvent) override;
	// End of UObject interface
	
	UPROPERTY(Config, EditAnywhere)
	TArray<TSoftClassPtr<UCSGlueGenerator>> Generators;
};
