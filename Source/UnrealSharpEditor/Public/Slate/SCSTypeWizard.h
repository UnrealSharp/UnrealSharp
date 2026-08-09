#pragma once

#include "CoreMinimal.h"
#include "Slate/CSProjectDestination.h"
#include "Widgets/SCompoundWidget.h"
#include "Widgets/Views/STableViewBase.h"
#include "Widgets/Views/STableRow.h"
#include "Widgets/Views/SListView.h"
#include "TypeWizardOption/CSTypeWizardOption.h"

class SEditableTextBox;
class SWidgetSwitcher;
class SWindow;
class ITableRow;

enum class ECSParentClassSource : uint8
{
	Common,
	AllClasses,
};

struct FCSCommonParentClassItem
{
	FCSCommonParentClassItem(UClass* InClass, FText InName, FText InTooltip)
		: Class(InClass), Name(MoveTemp(InName)), Tooltip(MoveTemp(InTooltip))
	{
	}
	
	UClass* Class = nullptr;
	FText Name;
	FText Tooltip;
};

class SCSTypeWizard : public SCompoundWidget
{
public:
	DECLARE_DELEGATE_TwoParams(FOnClassCreated, const FString&, const FString&);

	SLATE_BEGIN_ARGS(SCSTypeWizard)
		{}
		SLATE_ARGUMENT(FName, InitialTypeOption)
		SLATE_EVENT(FOnClassCreated, OnClassCreated)
	SLATE_END_ARGS()

	void Construct(const FArguments& InArgs);

	// SWidget interface implementation
	virtual FReply OnKeyDown(const FGeometry& MyGeometry, const FKeyEvent& InKeyEvent) override;
	virtual bool SupportsKeyboardFocus() const override { return true; }
	// End
	
	static void OpenDialog(const FOnClassCreated& OnClassCreated = FOnClassCreated());

private:
	TSharedRef<SWidget> BuildTypeOptionRow();
	TSharedRef<SWidget> BuildNameRow();
	TSharedRef<SWidget> BuildParentClassRow();
	TSharedRef<SWidget> BuildCommonClassList();
	TSharedRef<SWidget> BuildClassViewer();
	TSharedRef<SWidget> BuildProjectRow();
	TSharedRef<SWidget> BuildPathRow();

	void PopulateCommonParentClasses();
	TSharedRef<ITableRow> OnGenerateCommonClassRow(TSharedPtr<FCSCommonParentClassItem> Item, const TSharedRef<STableViewBase>& OwnerTable);
	void OnCommonClassSelectionChanged(TSharedPtr<FCSCommonParentClassItem> Item, ESelectInfo::Type SelectInfo);
	void OnClassPicked(UClass* PickedClass);
	
	void OnTypeNameChanged(const FText& NewText);
	void OnTypeNameCommitted(const FText& NewText, ETextCommit::Type CommitType);
	void OnPathChanged(const FText& NewText);
	void OnProjectDestinationChanged(TSharedPtr<FCSProjectDestination> NewProjectDestination, ESelectInfo::Type SelectInfo);
	void SetTargetPath(const FString& NewPath);
	FReply HandleBrowseForPath();

	FName GetSelectedTypeOptionName() const;
	void SetSelectedTypeOptionName(FName NewOptionName);
	bool NeedsParentClass() const { return SelectedTypeOption.IsValid() && SelectedTypeOption->RequiresParentClass(); }
	EVisibility GetParentClassVisibility() const { return NeedsParentClass() ? EVisibility::Visible : EVisibility::Collapsed; }

	ECSParentClassSource GetParentClassSource() const { return ParentClassSource; }
	void SetParentClassSource(ECSParentClassSource NewSource);
	
	void UpdateValidity();
	bool IsCreateEnabled() const { return bIsValid; }
	EVisibility GetErrorVisibility() const { return bIsValid ? EVisibility::Collapsed : EVisibility::Visible; }
	FText GetErrorText() const { return ValidationError; }

	FCSNewTypeParams MakeTypeParams() const;
	FString GetManagedTypeName() const;
	FText GetTypeOptionDisplayName() const;
	FText GetNameHintText() const;
	FText GetDeclarationText() const;
	FText GetNamespaceText() const;
	FText GetFilePathText() const;
	FString GetTargetFilePath() const;
	
	FReply HandleCreateClicked();
	FReply HandleCancelClicked();
	void CloseContainingWindow();

	EActiveTimerReturnType SetFocusPostConstruct(double InCurrentTime, float InDeltaTime);

private:
	FOnClassCreated OnClassCreated;

	TSharedPtr<SEditableTextBox> NameTextBox;
	TSharedPtr<SEditableTextBox> PathTextBox;
	TSharedPtr<SCSProjectDestinationPicker> ProjectDestinationPicker;
	TSharedPtr<SWidgetSwitcher> ParentClassSwitcher;
	TSharedPtr<SListView<TSharedPtr<FCSCommonParentClassItem>>> CommonClassListView;
	TSharedPtr<SWidget> ClassViewerWidget;

	TArray<TSharedPtr<FCSCommonParentClassItem>> CommonParentClasses;

	FString NewTypeName;
	FString NewTypePath;
	TWeakObjectPtr<UClass> SelectedParentClass;
	bool bHasExplicitParentSelection = false;

	TSharedPtr<FCSTypeWizardOption> SelectedTypeOption;
	ECSParentClassSource ParentClassSource = ECSParentClassSource::Common;

	bool bIsValid = false;
	FText ValidationError;
};
