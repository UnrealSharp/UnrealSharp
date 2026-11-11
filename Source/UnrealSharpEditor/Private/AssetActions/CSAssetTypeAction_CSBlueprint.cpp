#include "AssetActions/CSAssetTypeAction_CSBlueprint.h"
#include "Types/CSBlueprint.h"
#include "CSSourceCodeNavigation.h"

UClass* FCSAssetTypeAction_CSBlueprint::GetSupportedClass() const
{
	return UCSBlueprint::StaticClass();
}

void FCSAssetTypeAction_CSBlueprint::OpenAssetEditor(const TArray<UObject*>& InObjects, TSharedPtr<class IToolkitHost> EditWithinLevelEditor)
{
	for (UObject* Object : InObjects)
	{
		if (const UCSBlueprint* Blueprint = Cast<UCSBlueprint>(Object))
		{
			FCSSourceCodeNavigation::OpenClass(Blueprint->GeneratedClass);
		}
	}
}

bool FCSAssetTypeAction_CSBlueprint::SupportsOpenedMethod(const EAssetTypeActivationOpenedMethod OpenedMethod) const
{
	return OpenedMethod == EAssetTypeActivationOpenedMethod::Edit;
}

FText FCSAssetTypeAction_CSBlueprint::GetName() const
{
	return FText::FromString(FString::Printf(TEXT("C# Class")));
}
