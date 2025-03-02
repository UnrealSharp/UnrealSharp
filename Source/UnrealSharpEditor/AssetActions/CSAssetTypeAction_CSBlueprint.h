#pragma once
#include "AssetTypeActions/AssetTypeActions_Blueprint.h"

class FCSAssetTypeAction_CSBlueprint : public FAssetTypeActions_Blueprint
{
public:
	// IAssetTypeActions interface
	virtual UClass* GetSupportedClass() const override;
	virtual void OpenAssetEditor(const TArray<UObject*>& InObjects, const EAssetTypeActivationOpenedMethod OpenedMethod, TSharedPtr<IToolkitHost> EditWithinLevelEditor = TSharedPtr<IToolkitHost>()) override;
	// End of IAssetTypeActions interface
};
