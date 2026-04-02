#pragma once
#include "AssetTypeActions/AssetTypeActions_Blueprint.h"

class FCSAssetTypeAction_CSBlueprint : public FAssetTypeActions_Blueprint
{
public:
	// IAssetTypeActions interface
	virtual UClass* GetSupportedClass() const override;
#if ENGINE_MAJOR_VERSION * 100 + ENGINE_MINOR_VERSION < 505
	virtual void OpenAssetEditor(const TArray<UObject*>& InObjects, const EAssetTypeActivationOpenedMethod OpenedMethod, TSharedPtr<IToolkitHost> EditWithinLevelEditor = TSharedPtr<IToolkitHost>()) override;
#else
	virtual void OpenAssetEditor(const TArray<UObject*>& InObjects, TSharedPtr<class IToolkitHost> EditWithinLevelEditor = TSharedPtr<IToolkitHost>()) override;
#endif
	virtual bool SupportsOpenedMethod(const EAssetTypeActivationOpenedMethod OpenedMethod) const override;
	virtual FText GetName() const override;
	// End of IAssetTypeActions interface
};
