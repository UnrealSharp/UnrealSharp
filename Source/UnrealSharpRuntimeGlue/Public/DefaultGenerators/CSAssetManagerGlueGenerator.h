// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSGlueGenerator.h"
#include "CSAssetManagerGlueGenerator.generated.h"

UCLASS(DisplayName="Asset Manager Glue Generator", NotBlueprintable, NotBlueprintType)
class UCSAssetManagerGlueGenerator : public UCSGlueGenerator
{
	GENERATED_BODY()
private:
	// UCSGlueGenerator interface
	virtual void Initialize() override;
	virtual void ForceRefresh() override
	{
		ProcessAssetIds();
		ProcessAssetTypes();
	}
	// End of UCSGlueGenerator interface

	void TryRegisterAssetTypes();
	
	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	void OnCompletedInitialScan();

	void OnAssetRemoved(const FAssetData& AssetData);
	void OnAssetRenamed(const FAssetData& AssetData, const FString& OldObjectPath);
	void OnInMemoryAssetCreated(UObject* Object);
	void OnInMemoryAssetDeleted(UObject* Object);

	void OnAssetSearchRootAdded(const FString& RootPath);

	void OnAssetManagerSettingsChanged(UObject* Object, FPropertyChangedEvent& PropertyChangedEvent);

	bool IsRegisteredAssetType(const FAssetData& AssetData) { return IsRegisteredAssetType(AssetData.GetClass()); }
	bool IsRegisteredAssetType(UClass* Class);

	void WaitUpdateAssetTypes()
	{
		GEditor->GetTimerManager()->SetTimerForNextTick(FTimerDelegate::CreateUObject(this, &ThisClass::ProcessAssetIds));
	}

	void ProcessAssetIds();
	void ProcessAssetTypes();

	bool bHasRegisteredAssetTypes = false;
};
