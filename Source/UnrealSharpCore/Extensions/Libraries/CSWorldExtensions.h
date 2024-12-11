// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSWorldExtensions.generated.h"

USTRUCT()
struct FCSSpawnActorParameters
{
	GENERATED_BODY()
	
	UPROPERTY()
	AActor* Owner;

	UPROPERTY()
	APawn* Instigator;

	UPROPERTY()
	AActor* Template;

	UPROPERTY()
	bool DeferConstruction;

	UPROPERTY()
	ESpawnActorCollisionHandlingMethod SpawnMethod;
};

UCLASS(meta = (Internal))
class UNREALSHARPCORE_API UCSWorldExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta = (ScriptMethod))
	static AActor* SpawnActor(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters);
};
