#pragma once

struct FCSProjectDestination
{
    FCSProjectDestination(const FName InKey, FText InDisplayName, FString InName, FString InPath, const int32 Index, TSharedPtr<IPlugin> Plugin = nullptr) :
        Key(InKey), DisplayName(MoveTemp(InDisplayName)), Name(MoveTemp(InName)), Path(MoveTemp(InPath)), Index(Index), Plugin(MoveTemp(Plugin)) {}

    FName GetKey() const { return Key; }
    const FText& GetDisplayName() const { return DisplayName; }
	const FString& GetName() const { return Name; }
    const FString& GetPath() const { return Path; }
    int32 GetIndex() const { return Index; }
    const TSharedPtr<IPlugin>& GetPlugin() const { return Plugin; }

private:
    FName Key;
    FText DisplayName;
	FString Name;
    FString Path;
    int32 Index;
    TSharedPtr<IPlugin> Plugin;

    friend uint32 GetTypeHash(const FCSProjectDestination& ProjectDestination)
    {
        return GetTypeHash(ProjectDestination.Key);
    }
};

class SCSNewProjectDialog : public SCompoundWidget
{

public:

	SLATE_BEGIN_ARGS(SCSNewProjectDialog) {}
	SLATE_END_ARGS()

	void Construct(const FArguments& InArgs);

private:

    void OnProjectDestinationChanged(TSharedPtr<FCSProjectDestination> NewProjectDestination, ESelectInfo::Type SelectInfo);
    static TSharedRef<SWidget> OnGenerateProjectDestinationWidget(TSharedRef<FCSProjectDestination> Destination);
	void OnPathSelected(const FString& NewPath);
	FReply OnExplorerButtonClicked();

	void OnCancel();
	void OnFinish();

	bool CanFinish() const;

	void CloseWindow();

private:

	TSharedPtr<SEditableTextBox> PathTextBox;
    TSharedPtr<SComboBox<TSharedRef<FCSProjectDestination>>> ProjectDestinationComboBox;
	TSharedPtr<SEditableTextBox> NameTextBox;

	FString SuggestedProjectName;
    TArray<TSharedRef<FCSProjectDestination>> ProjectDestinations;
    int32 SelectedProjectDestinationIndex = INDEX_NONE;

};
