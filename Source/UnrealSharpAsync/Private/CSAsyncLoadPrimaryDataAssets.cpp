#include "CSAsyncLoadPrimaryDataAssets.h"
#include "Engine/AssetManager.h"

void UCSAsyncLoadPrimaryDataAssets::LoadPrimaryDataAssets(const TArray<FPrimaryAssetId>& AssetIds, const TArray<FName>& AssetBundles)
{
	UAssetManager& AssetManager = UAssetManager::Get();
	AssetManager.LoadPrimaryAssets(AssetIds, AssetBundles, FStreamableDelegate::CreateUObject(this, &UCSAsyncLoadPrimaryDataAssets::OnPrimaryDataAssetsLoaded));
}

void UCSAsyncLoadPrimaryDataAssets::OnPrimaryDataAssetsLoaded()
{
	InvokeManagedCallback();
}
