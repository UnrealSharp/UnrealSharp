#pragma once

#include "CoreMinimal.h"
#include "Slate/CSProjectDestination.h"
#include "Widgets/SCompoundWidget.h"

class SEditableTextBox;
class SCheckBox;

class SCSNewProjectDialog : public SCompoundWidget
{
public:
	SLATE_BEGIN_ARGS(SCSNewProjectDialog) {}
	SLATE_END_ARGS()

	void Construct(const FArguments& InArgs);

private:

	void OnProjectDestinationChanged(TSharedPtr<FCSProjectDestination> NewProjectDestination, ESelectInfo::Type SelectInfo);
	void OnPathSelected(const FString& NewPath);
	FReply OnExplorerButtonClicked();

	void OnCancel();
	void OnFinish();

	FText GetValidationError() const;
	bool CanFinish() const;

	void CloseWindow();

private:

	TSharedPtr<SEditableTextBox> PathTextBox;
	TSharedPtr<SCSProjectDestinationPicker> ProjectDestinationPicker;
	TSharedPtr<SEditableTextBox> NameTextBox;
	TSharedPtr<SCheckBox> EditorOnlyCheckBox;

	FString SuggestedProjectName;

};