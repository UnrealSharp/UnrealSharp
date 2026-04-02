// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSGlueGenerator.h"
#include "CSGameplayTagsGlueGenerator.generated.h"

UCLASS(DisplayName="Gameplay Tags Glue Generator", NotBlueprintable, NotBlueprintType)
class UCSGameplayTagsGlueGenerator : public UCSGlueGenerator
{
	GENERATED_BODY()
private:
	// UCSGlueGenerator interface
	virtual void Initialize() override;
	virtual void ForceRefresh() override { ProcessGameplayTags(); }
	// End of UCSGlueGenerator interface

	void ProcessGameplayTags();
};
