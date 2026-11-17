#pragma once

#include "AssetTypeActions_Base.h"

// Generated structs and enums must not be opened in Unreal's mutable asset editors.
class FCSAssetTypeAction_ReadOnly : public FAssetTypeActions_Base
{
public:
	FCSAssetTypeAction_ReadOnly(UClass* InSupportedClass, const FText& InName)
		: SupportedClass(InSupportedClass), Name(InName)
	{
	}

	virtual UClass* GetSupportedClass() const override { return SupportedClass; }
	virtual FText GetName() const override { return Name; }
	virtual FColor GetTypeColor() const override { return FColor::White; }
	virtual uint32 GetCategories() override { return EAssetTypeCategories::Misc; }
	virtual bool SupportsOpenedMethod(const EAssetTypeActivationOpenedMethod OpenedMethod) const override { return false; }
	virtual bool CanRename(const FAssetData& Asset, FText* OutErrorMsg) const override { return false; }
	virtual bool CanDuplicate(const FAssetData& Asset, FText* OutErrorMsg) const override { return false; }
	virtual bool CanLocalize() const override { return false; }

private:
	UClass* SupportedClass;
	FText Name;
};
