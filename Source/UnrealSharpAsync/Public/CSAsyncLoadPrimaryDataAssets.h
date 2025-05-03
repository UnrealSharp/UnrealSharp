// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSAsyncActionBase.h"
#include "CSAsyncLoadPrimaryDataAssets.generated.h"

UCLASS()
class UCSAsyncLoadPrimaryDataAssets : public UCSAsyncActionBase
{
	GENERATED_BODY()
public:
	UFUNCTION(meta =(ScriptMethod))
	void LoadPrimaryDataAssets(const TArray<FPrimaryAssetId>& AssetIds, const TArray<FName>& AssetBundles);
private:
	void OnPrimaryDataAssetsLoaded();
};
