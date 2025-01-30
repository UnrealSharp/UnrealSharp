#include "CSAssetTypeAction_CSBlueprint.h"

#include "SourceCodeNavigation.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"

UClass* FCSAssetTypeAction_CSBlueprint::GetSupportedClass() const
{
	return UCSBlueprint::StaticClass();
}

void FCSAssetTypeAction_CSBlueprint::OpenAssetEditor(const TArray<UObject*>& InObjects, const EAssetTypeActivationOpenedMethod OpenedMethod, TSharedPtr<IToolkitHost> EditWithinLevelEditor)
{
	UCSBlueprint* Blueprint = Cast<UCSBlueprint>(InObjects[0]);

	FString OutFilePath;
	FCSTypeRegistry::Get().GetClassFilePath(Blueprint->GetFName(), OutFilePath);

	if (OutFilePath.IsEmpty())
	{
		return;
	}

	// TODO: Replace this to open in the correct solution instance for C#. This is just a placeholder.
	FString AbsoluteHeaderPath = FPaths::ProjectDir() + OutFilePath;
	FSourceCodeNavigation::OpenSourceFile(AbsoluteHeaderPath);
}
