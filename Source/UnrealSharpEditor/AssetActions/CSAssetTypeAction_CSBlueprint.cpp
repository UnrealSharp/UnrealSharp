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

	// TODO: Add support for opening the file in an external editor.
	// Currently Rider doesn't support opening files from the command line as it opens the file in every instance of Rider, even if I have specified the solution to open it in.
	// A ticket has been created to try to get it fixed.
}
