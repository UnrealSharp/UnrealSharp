#pragma once

class SCSNewProjectDialog : public SCompoundWidget
{
	
public:
	
	SLATE_BEGIN_ARGS(SCSNewProjectDialog) {}
		SLATE_ATTRIBUTE(FString, SuggestedProjectName)
	SLATE_END_ARGS()
	
	void Construct(const FArguments& InArgs);

private:

	TSharedPtr<SEditableTextBox> PathTextBox;
	TSharedPtr<SEditableTextBox> NameTextBox;
	
	FString ScriptPath;

	FString SuggestedProjectName;
	bool bOpenSolution;
	
	void OnPathSelected(const FString& NewPath);
	FReply OnExplorerButtonClicked();

	void OnCancel();
	void OnFinish();

	bool CanFinish() const;

	void CloseWindow();
	
};
