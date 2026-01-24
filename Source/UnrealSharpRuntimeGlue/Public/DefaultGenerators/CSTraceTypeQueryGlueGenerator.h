// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSGlueGenerator.h"
#include "CSTraceTypeQueryGlueGenerator.generated.h"

UCLASS(DisplayName="Trace Type Query Glue Generator", NotBlueprintable, NotBlueprintType)
class UCSTraceTypeQueryGlueGenerator : public UCSGlueGenerator
{
	GENERATED_BODY()

	// UCSGlueGenerator interface
	virtual void Initialize() override;
	virtual void ForceRefresh() override { ProcessCollisionProfile(); }
	// End of UCSGlueGenerator interface

	void OnCollisionProfileChanged(UCollisionProfile* CollisionProfile);
	
	void ProcessCollisionProfile();
};
