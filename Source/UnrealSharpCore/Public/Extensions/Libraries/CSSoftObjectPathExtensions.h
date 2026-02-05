// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSSoftObjectPathExtensions.generated.h"

UCLASS(meta = (InternalType))
class UCSSoftObjectPathExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static UObject* ResolveObject(const FSoftObjectPath& SoftObjectPath);
	
	UFUNCTION(meta=(ScriptMethod))
	static FPrimaryAssetId GetPrimaryAssetId(const FSoftObjectPath& SoftObjectPath);
};
