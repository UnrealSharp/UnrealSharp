#pragma once

struct FProjectDestination
{
    FProjectDestination(const FName InKey, FText InDisplayName, FString InPath, const int32 Index, TSharedPtr<IPlugin> Plugin = nullptr) :
        Key(InKey), DisplayName(MoveTemp(InDisplayName)), Path(MoveTemp(InPath)), Index(Index), Plugin(MoveTemp(Plugin)) {}

    FName GetKey() const { return Key; }
    const FText& GetDisplayName() const { return DisplayName; }
    const FString& GetPath() const { return Path; }
    int32 GetIndex() const { return Index; }
    const TSharedPtr<IPlugin>& GetPlugin() const { return Plugin; }

private:
    FName Key;
    FText DisplayName;
    FString Path;
    int32 Index;
    TSharedPtr<IPlugin> Plugin;

    friend uint32 GetTypeHash(const FProjectDestination& ProjectDestination)
    {
        return GetTypeHash(ProjectDestination.Key);
    }
};

class SCSNewProjectDialog : public SCompoundWidget
{

public:

	SLATE_BEGIN_ARGS(SCSNewProjectDialog) {}
		SLATE_ATTRIBUTE(FString, SuggestedProjectName)
	SLATE_END_ARGS()

	void Construct(const FArguments& InArgs);

private:

    void OnProjectDestinationChanged(TSharedPtr<FProjectDestination> NewProjectDestination, ESelectInfo::Type SelectInfo);
    static TSharedRef<SWidget> OnGenerateProjectDestinationWidget(TSharedRef<FProjectDestination> Destination);
	void OnPathSelected(const FString& NewPath);
	FReply OnExplorerButtonClicked();

	void OnCancel();
	void OnFinish();

	bool CanFinish() const;

	void CloseWindow();

private:

	TSharedPtr<SEditableTextBox> PathTextBox;
    TSharedPtr<SComboBox<TSharedRef<FProjectDestination>>> ProjectDestinationComboBox;
	TSharedPtr<SEditableTextBox> NameTextBox;

	FString SuggestedProjectName;
    TArray<TSharedRef<FProjectDestination>> ProjectDestinations;
    int32 SelectedProjectDestinationIndex = INDEX_NONE;

};
