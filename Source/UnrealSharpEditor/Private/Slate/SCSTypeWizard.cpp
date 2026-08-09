#include "Slate/SCSTypeWizard.h"

#include "ClassViewerFilter.h"
#include "ClassViewerModule.h"
#include "CSPathsUtilities.h"
#include "CSUnrealSharpEditorSettings.h"
#include "DesktopPlatformModule.h"
#include "IDesktopPlatform.h"
#include "SPrimaryButton.h"
#include "SWarningOrErrorBox.h"
#include "Blueprint/UserWidget.h"
#include "Components/ActorComponent.h"
#include "Components/SceneComponent.h"
#include "Framework/Application/SlateApplication.h"
#include "GameFramework/Actor.h"
#include "GameFramework/Character.h"
#include "GameFramework/GameMode.h"
#include "GameFramework/GameState.h"
#include "GameFramework/Pawn.h"
#include "GameFramework/PlayerController.h"
#include "Interfaces/IMainFrameModule.h"
#include "Misc/MessageDialog.h"
#include "Misc/Paths.h"
#include "Modules/ModuleManager.h"
#include "Slate/TypeWizardOption/CSTypeGenerator.h"
#include "Slate/TypeWizardOption/CSTypeWizardOptionRegistry.h"
#include "Styling/AppStyle.h"
#include "Utilities/CSClassUtilities.h"
#include "Widgets/SBoxPanel.h"
#include "Widgets/SWindow.h"
#include "Widgets/Images/SImage.h"
#include "Widgets/Input/SButton.h"
#include "Widgets/Input/SCheckBox.h"
#include "Widgets/Input/SEditableTextBox.h"
#include "Widgets/Input/SSegmentedControl.h"
#include "Widgets/Layout/SBorder.h"
#include "Widgets/Layout/SBox.h"
#include "Widgets/Layout/SGridPanel.h"
#include "Widgets/Layout/SUniformGridPanel.h"
#include "Widgets/Layout/SWidgetSwitcher.h"
#include "Widgets/Text/STextBlock.h"

#define LOCTEXT_NAMESPACE "UnrealSharpNewClassDialog"

namespace SCSNewTypeWizardPrivate
{
	static constexpr float LabelColumnWidth = 96.f;
	static const FMargin RowPadding(0.f, 4.f);
}

class FParentClassFilter : public IClassViewerFilter
{
public:
	virtual bool IsClassAllowed(const FClassViewerInitializationOptions& InInitOptions, const UClass* InClass, TSharedRef<FClassViewerFilterFuncs> InFilterFuncs) override
	{
		if (!InClass || InClass->HasAnyClassFlags(CLASS_Deprecated | CLASS_NewerVersionExists | CLASS_Hidden | CLASS_HideDropDown))
		{
			return false;
		}
			
		if (InClass->GetName().StartsWith(TEXT("SKEL_")) || InClass->GetName().StartsWith(TEXT("REINST_")))
		{
			return false;
		}

		return !FCSClassUtilities::IsBlueprintObject(InClass);
	}

	virtual bool IsUnloadedClassAllowed(const FClassViewerInitializationOptions& InInitOptions, const TSharedRef<const IUnloadedBlueprintData> InUnloadedClassData, TSharedRef<FClassViewerFilterFuncs> InFilterFuncs) override
	{
		return false;
	}
};

void SCSTypeWizard::Construct(const FArguments& InArgs)
{
	OnClassCreated = InArgs._OnClassCreated;
	SelectedTypeOption = FCSTypeWizardOptionRegistry::Get().GetDefaultOption();
	NewTypePath = FPaths::ConvertRelativePathToFull(UnrealSharp::Paths::GetScriptFolderDirectory());
	SelectedParentClass = UObject::StaticClass();

	FPaths::NormalizeDirectoryName(NewTypePath);
	PopulateCommonParentClasses();

	ChildSlot
	[
		SNew(SBorder)
		.BorderImage(FAppStyle::GetBrush("Brushes.Panel"))
		.Padding(16.f)
		[
			SNew(SVerticalBox)
			+ SVerticalBox::Slot()
			.AutoHeight()
			.Padding(0.f, 0.f, 0.f, 8.f)
			[
				BuildTypeOptionRow()
			]
			+ SVerticalBox::Slot()
			.AutoHeight()
			[
				BuildNameRow()
			]
			+ SVerticalBox::Slot()
			.FillHeight(1.f)
			.Padding(0.f, 8.f)
			[
				BuildParentClassRow()
			]
			+ SVerticalBox::Slot()
			.AutoHeight()
			[
				BuildProjectRow()
			]
			+ SVerticalBox::Slot()
			.AutoHeight()
			[
				BuildPathRow()
			]
			+ SVerticalBox::Slot()
			.AutoHeight()
			.Padding(0.f, 8.f, 0.f, 0.f)
			[
				SNew(SWarningOrErrorBox)
				.MessageStyle(EMessageStyle::Error)
				.Visibility(this, &SCSTypeWizard::GetErrorVisibility)
				.Message(this, &SCSTypeWizard::GetErrorText)
			]
			+ SVerticalBox::Slot()
			.AutoHeight()
			.HAlign(HAlign_Right)
			.Padding(0.f, 16.f, 0.f, 0.f)
			[
				SNew(SUniformGridPanel)
				.SlotPadding(FAppStyle::GetMargin("StandardDialog.SlotPadding"))
				.MinDesiredSlotWidth(FAppStyle::GetFloat("StandardDialog.MinDesiredSlotWidth"))
				.MinDesiredSlotHeight(FAppStyle::GetFloat("StandardDialog.MinDesiredSlotHeight"))

				+ SUniformGridPanel::Slot(0, 0)
				[
					SNew(SPrimaryButton)
					.Text(LOCTEXT("Create", "Create"))
					.IsEnabled(this, &SCSTypeWizard::IsCreateEnabled)
					.OnClicked(this, &SCSTypeWizard::HandleCreateClicked)
				]

				+ SUniformGridPanel::Slot(1, 0)
				[
					SNew(SButton)
					.HAlign(HAlign_Center)
					.VAlign(VAlign_Center)
					.Text(LOCTEXT("Cancel", "Cancel"))
					.OnClicked(this, &SCSTypeWizard::HandleCancelClicked)
				]
			]
		]
	];

	OnProjectDestinationChanged(ProjectDestinationPicker->GetSelectedDestination(), ESelectInfo::Direct);

	UpdateValidity();
	RegisterActiveTimer(0.f, FWidgetActiveTimerDelegate::CreateSP(this, &SCSTypeWizard::SetFocusPostConstruct));
}

TSharedRef<SWidget> SCSTypeWizard::BuildTypeOptionRow()
{
	const TArray<TSharedRef<FCSTypeWizardOption>>& TypeOptions = FCSTypeWizardOptionRegistry::Get().GetOptions();

	SSegmentedControl<FName>::FArguments SegmentedArgs;
	SegmentedArgs.Value(this, &SCSTypeWizard::GetSelectedTypeOptionName);
	SegmentedArgs.OnValueChanged(this, &SCSTypeWizard::SetSelectedTypeOptionName);

	for (const TSharedRef<FCSTypeWizardOption>& TypeOption : TypeOptions)
	{
		SegmentedArgs + SSegmentedControl<FName>::Slot(TypeOption->GetOptionName())
			.Text(TypeOption->GetDisplayName());
	}

	return SNew(SHorizontalBox)
		.Visibility(TypeOptions.Num() > 1 ? EVisibility::Visible : EVisibility::Collapsed)
		+ SHorizontalBox::Slot()
		.AutoWidth()
		.VAlign(VAlign_Center)
		[
			SNew(SBox)
			.WidthOverride(SCSNewTypeWizardPrivate::LabelColumnWidth)
			[
				SNew(STextBlock).Text(LOCTEXT("TypeLabel", "Type"))
			]
		]
		+ SHorizontalBox::Slot()
		.FillWidth(1.f)
		[
			SArgumentNew(SegmentedArgs, SSegmentedControl<FName>)
		];
}

TSharedRef<SWidget> SCSTypeWizard::BuildNameRow()
{
	return SNew(SHorizontalBox)
		+ SHorizontalBox::Slot()
		.AutoWidth()
		.VAlign(VAlign_Center)
		[
			SNew(SBox)
			.WidthOverride(SCSNewTypeWizardPrivate::LabelColumnWidth)
			[
				SNew(STextBlock).Text(LOCTEXT("NameLabel", "Name"))
			]
		]
		+ SHorizontalBox::Slot()
		.FillWidth(1.f)
		[
			SAssignNew(NameTextBox, SEditableTextBox)
			.Text(FText::FromString(NewTypeName))
			.HintText(this, &SCSTypeWizard::GetNameHintText)
			.SelectAllTextWhenFocused(true)
			.OnTextChanged(this, &SCSTypeWizard::OnTypeNameChanged)
			.OnTextCommitted(this, &SCSTypeWizard::OnTypeNameCommitted)
		];
}

TSharedRef<SWidget> SCSTypeWizard::BuildParentClassRow()
{
	return SNew(SVerticalBox)
		.Visibility(this, &SCSTypeWizard::GetParentClassVisibility)
		+ SVerticalBox::Slot()
		.AutoHeight()
		.HAlign(HAlign_Center)
		.Padding(0.f, 0.f, 0.f, 6.f)
		[
			SNew(SSegmentedControl<ECSParentClassSource>)
			.Value(this, &SCSTypeWizard::GetParentClassSource)
			.OnValueChanged(this, &SCSTypeWizard::SetParentClassSource)
			
			+ SSegmentedControl<ECSParentClassSource>::Slot(ECSParentClassSource::Common)
			.Text(LOCTEXT("CommonClasses", "Common"))
			
			+ SSegmentedControl<ECSParentClassSource>::Slot(ECSParentClassSource::AllClasses)
			.Text(LOCTEXT("AllClasses", "All Classes"))
		]
		+ SVerticalBox::Slot()
		.FillHeight(1.f)
		[
			SNew(SHorizontalBox)
			+ SHorizontalBox::Slot()
			.AutoWidth()
			.VAlign(VAlign_Top)
			.Padding(0.f, 4.f, 0.f, 0.f)
			[
				SNew(SBox)
				.WidthOverride(SCSNewTypeWizardPrivate::LabelColumnWidth)
				[
					SNew(STextBlock).Text(LOCTEXT("ParentClassLabel", "Parent Class"))
				]
			]

			+ SHorizontalBox::Slot()
			.FillWidth(1.f)
			[
				SNew(SBox)
				.MaxDesiredHeight(280.f)
				[
					SNew(SBorder)
					.BorderImage(FAppStyle::GetBrush("Brushes.Recessed"))
					.Padding(1.f)
					[
						SAssignNew(ParentClassSwitcher, SWidgetSwitcher)
						.WidgetIndex_Lambda([this]() { return ParentClassSource == ECSParentClassSource::Common ? 0 : 1; })
						+ SWidgetSwitcher::Slot()
						[
							BuildCommonClassList()
						]
						+ SWidgetSwitcher::Slot()
						[
							BuildClassViewer()
						]
					]
				]
			]
		];
}

void SCSTypeWizard::PopulateCommonParentClasses()
{
	auto AddCommonParentClass = [this](UClass* Class, const FText& Name, const FText& Tooltip)
	{
		CommonParentClasses.Add(MakeShared<FCSCommonParentClassItem>(Class, Name, Tooltip));
	};
	
	auto AddDefaultParentClass = [AddCommonParentClass](UClass* ParentClass)
	{
		FText DisplayName = ParentClass->GetDisplayNameText();
		FText ToolTip = ParentClass->GetToolTipText();
		AddCommonParentClass(ParentClass, DisplayName, ToolTip);
	};
	
	AddDefaultParentClass(UObject::StaticClass());
	AddDefaultParentClass(AActor::StaticClass());
	AddDefaultParentClass(UActorComponent::StaticClass());
	AddDefaultParentClass(USceneComponent::StaticClass());
	AddDefaultParentClass(APawn::StaticClass());
	AddDefaultParentClass(ACharacter::StaticClass());
	AddDefaultParentClass(APlayerController::StaticClass());
	AddDefaultParentClass(UUserWidget::StaticClass());
	AddDefaultParentClass(AGameModeBase::StaticClass());
	AddDefaultParentClass(AGameMode::StaticClass());
	AddDefaultParentClass(AGameStateBase::StaticClass());
	AddDefaultParentClass(AGameState::StaticClass());
	
	const UCSUnrealSharpEditorSettings* UnrealSharpEditorSettings = GetDefault<UCSUnrealSharpEditorSettings>();
	
	for (const TSoftClassPtr<UObject>& Item : UnrealSharpEditorSettings->CommonParentClasses)
	{
		UClass* ParentClass = Item.Get();
		
		if (!IsValid(ParentClass) || FCSClassUtilities::IsBlueprintObject(ParentClass))
		{
			continue;
		}
		
		AddDefaultParentClass(ParentClass);
	}
	
	AddCommonParentClass(nullptr, LOCTEXT("Empty", "Plain C# Class"), LOCTEXT("EmptyTooltip", "A plain C# class with no reflection attributes and no base type."));
}

TSharedRef<SWidget> SCSTypeWizard::BuildCommonClassList()
{
	SAssignNew(CommonClassListView, SListView<TSharedPtr<FCSCommonParentClassItem>>)
	.ListItemsSource(&CommonParentClasses)
	.SelectionMode(ESelectionMode::Single)
	.OnGenerateRow(this, &SCSTypeWizard::OnGenerateCommonClassRow)
	.OnSelectionChanged(this, &SCSTypeWizard::OnCommonClassSelectionChanged);

	return CommonClassListView.ToSharedRef();
}

TSharedRef<ITableRow> SCSTypeWizard::OnGenerateCommonClassRow(TSharedPtr<FCSCommonParentClassItem> Item, const TSharedRef<STableViewBase>& OwnerTable)
{
	return SNew(STableRow<TSharedPtr<FCSCommonParentClassItem>>, OwnerTable)
		.ToolTipText(Item->Tooltip)
		.Padding(FMargin(6.f, 3.f))
		[
			SNew(STextBlock).Text(Item->Name)
		];
}

void SCSTypeWizard::OnCommonClassSelectionChanged(TSharedPtr<FCSCommonParentClassItem> Item, ESelectInfo::Type SelectInfo)
{
	if (!Item.IsValid())
	{
		return;
	}

	SelectedParentClass = Item->Class;
	bHasExplicitParentSelection = true;
	UpdateValidity();
}

TSharedRef<SWidget> SCSTypeWizard::BuildClassViewer()
{
	FClassViewerInitializationOptions Options;
	Options.Mode = EClassViewerMode::ClassPicker;
	Options.DisplayMode = EClassViewerDisplayMode::ListView;
	Options.bShowNoneOption = false;
	Options.bShowUnloadedBlueprints = false;
	Options.bShowObjectRootClass = true;
	Options.bShowBackgroundBorder = false;
	Options.bAllowViewOptions = true;
	Options.bExpandAllNodes = false;
	Options.ClassFilters.Add(MakeShared<FParentClassFilter>());
	Options.InitiallySelectedClass = SelectedParentClass.Get();

	FClassViewerModule& ClassViewerModule = FModuleManager::LoadModuleChecked<FClassViewerModule>("ClassViewer");
	ClassViewerWidget = ClassViewerModule.CreateClassViewer(Options, FOnClassPicked::CreateSP(this, &SCSTypeWizard::OnClassPicked));

	return ClassViewerWidget.ToSharedRef();
}

void SCSTypeWizard::OnClassPicked(UClass* PickedClass)
{
	SelectedParentClass = PickedClass;
	bHasExplicitParentSelection = PickedClass != nullptr;
	UpdateValidity();
}

FName SCSTypeWizard::GetSelectedTypeOptionName() const
{
	return SelectedTypeOption.IsValid() ? SelectedTypeOption->GetOptionName() : NAME_None;
}

void SCSTypeWizard::SetSelectedTypeOptionName(FName NewOptionName)
{
	TSharedPtr<FCSTypeWizardOption> NewTypeOption = FCSTypeWizardOptionRegistry::Get().FindOption(NewOptionName);
	
	if (!NewTypeOption.IsValid())
	{
		return;
	}

	SelectedTypeOption = NewTypeOption;
	UpdateValidity();
}

void SCSTypeWizard::SetParentClassSource(ECSParentClassSource NewSource)
{
	ParentClassSource = NewSource;
	
	if (NewSource == ECSParentClassSource::AllClasses && !bHasExplicitParentSelection)
	{
		SelectedParentClass = nullptr;
	}

	UpdateValidity();
}

TSharedRef<SWidget> SCSTypeWizard::BuildProjectRow()
{
	return SNew(SHorizontalBox)

		+ SHorizontalBox::Slot()
		.AutoWidth()
		.VAlign(VAlign_Center)
		[
			SNew(SBox)
			.WidthOverride(SCSNewTypeWizardPrivate::LabelColumnWidth)
			[
				SNew(STextBlock).Text(LOCTEXT("ProjectLabel", "Project"))
			]
		]

		+ SHorizontalBox::Slot()
		.FillWidth(1.f)
		.Padding(SCSNewTypeWizardPrivate::RowPadding)
		[
			SAssignNew(ProjectDestinationPicker, SCSProjectDestinationPicker)
			.Mode(ECSProjectDestinationMode::Projects)
			.OnDestinationChanged(this, &SCSTypeWizard::OnProjectDestinationChanged)
		];
}

TSharedRef<SWidget> SCSTypeWizard::BuildPathRow()
{
	auto MakeInfoRow = [](const FText& Label, TAttribute<FText> Value)
	{
		return SNew(SHorizontalBox)

			+ SHorizontalBox::Slot()
			.AutoWidth()
			[
				SNew(SBox)
				.WidthOverride(SCSNewTypeWizardPrivate::LabelColumnWidth)
				[
					SNew(STextBlock).Text(Label)
				]
			]

			+ SHorizontalBox::Slot()
			.FillWidth(1.f)
			[
				SNew(STextBlock)
				.Text(Value)
				.ColorAndOpacity(FSlateColor::UseSubduedForeground())
			];
	};

	return SNew(SVerticalBox)
		+ SVerticalBox::Slot()
		.AutoHeight()
		.Padding(SCSNewTypeWizardPrivate::RowPadding)
		[
			SNew(SHorizontalBox)
			+ SHorizontalBox::Slot()
			.AutoWidth()
			.VAlign(VAlign_Center)
			[
				SNew(SBox)
				.WidthOverride(SCSNewTypeWizardPrivate::LabelColumnWidth)
				[
					SNew(STextBlock).Text(LOCTEXT("PathLabel", "Path"))
				]
			]

			+ SHorizontalBox::Slot()
			.FillWidth(1.f)
			[
				SAssignNew(PathTextBox, SEditableTextBox)
				.Text(FText::FromString(NewTypePath))
				.OnTextChanged(this, &SCSTypeWizard::OnPathChanged)
			]

			+ SHorizontalBox::Slot()
			.AutoWidth()
			.Padding(4.f, 0.f, 0.f, 0.f)
			[
				SNew(SButton)
				.ButtonStyle(FAppStyle::Get(), "SimpleButton")
				.ToolTipText(LOCTEXT("BrowseTooltip", "Choose a folder"))
				.ContentPadding(2.f)
				.OnClicked(this, &SCSTypeWizard::HandleBrowseForPath)
				[
					SNew(SImage)
					.Image(FAppStyle::GetBrush("Icons.FolderClosed"))
					.ColorAndOpacity(FSlateColor::UseForeground())
				]
			]
		]

		+ SVerticalBox::Slot()
		.AutoHeight()
		.Padding(SCSNewTypeWizardPrivate::RowPadding)
		[
			MakeInfoRow(LOCTEXT("NamespaceLabel", "Namespace"),
			            TAttribute<FText>::CreateSP(this, &SCSTypeWizard::GetNamespaceText))
		]

		+ SVerticalBox::Slot()
		.AutoHeight()
		.Padding(SCSNewTypeWizardPrivate::RowPadding)
		[
			MakeInfoRow(LOCTEXT("DeclarationLabel", "Declaration"),
			            TAttribute<FText>::CreateSP(this, &SCSTypeWizard::GetDeclarationText))
		]

		+ SVerticalBox::Slot()
		.AutoHeight()
		.Padding(SCSNewTypeWizardPrivate::RowPadding)
		[
			MakeInfoRow(LOCTEXT("FileLabel", "File"),
			            TAttribute<FText>::CreateSP(this, &SCSTypeWizard::GetFilePathText))
		];
}

void SCSTypeWizard::OnTypeNameChanged(const FText& NewText)
{
	NewTypeName = NewText.ToString();
	UpdateValidity();
}

void SCSTypeWizard::OnTypeNameCommitted(const FText& NewText, ETextCommit::Type CommitType)
{
	if (CommitType != ETextCommit::OnEnter || !bIsValid)
	{
		return;
	}
	
	HandleCreateClicked();
}

void SCSTypeWizard::OnPathChanged(const FText& NewText)
{
	NewTypePath = NewText.ToString();

	if (ProjectDestinationPicker.IsValid())
	{
		ProjectDestinationPicker->SelectDestinationForPath(NewTypePath);
	}

	UpdateValidity();
}

void SCSTypeWizard::OnProjectDestinationChanged(TSharedPtr<FCSProjectDestination> NewProjectDestination, ESelectInfo::Type SelectInfo)
{
	if (!NewProjectDestination.IsValid())
	{
		UpdateValidity();
		return;
	}

	SetTargetPath(NewProjectDestination->GetPath());
}

void SCSTypeWizard::SetTargetPath(const FString& NewPath)
{
	NewTypePath = NewPath;
	FPaths::NormalizeDirectoryName(NewTypePath);

	if (PathTextBox.IsValid())
	{
		PathTextBox->SetText(FText::FromString(NewTypePath));
	}

	UpdateValidity();
}

FReply SCSTypeWizard::HandleBrowseForPath()
{
	const void* ParentWindowHandle = FSlateApplication::Get().FindBestParentWindowHandleForDialogs(AsShared());

	FString ChosenFolder;
	const bool bHasPickedFolder = FDesktopPlatformModule::Get()->OpenDirectoryDialog(
		ParentWindowHandle,
		LOCTEXT("ChooseFolderTitle", "Choose a folder for the new file").ToString(),
		NewTypePath,
		ChosenFolder);

	if (bHasPickedFolder)
	{
		if (ProjectDestinationPicker.IsValid())
		{
			ProjectDestinationPicker->SelectDestinationForPath(ChosenFolder);
		}

		SetTargetPath(ChosenFolder);
	}

	return FReply::Handled();
}

void SCSTypeWizard::UpdateValidity()
{
	bIsValid = false;
	ValidationError = FText::GetEmpty();

	if (!SelectedTypeOption.IsValid())
	{
		ValidationError = LOCTEXT("NoTypeOption", "No type option is registered.");
		return;
	}

	if (!FCSTypeGenerator::IsValidCSharpIdentifier(NewTypeName, ValidationError))
	{
		return;
	}

	if (NeedsParentClass() && ParentClassSource == ECSParentClassSource::AllClasses && !SelectedParentClass.IsValid())
	{
		ValidationError = LOCTEXT("NoParentSelected", "Select a parent class.");
		return;
	}

	if (!ProjectDestinationPicker.IsValid() || !ProjectDestinationPicker->HasDestinations())
	{
		ValidationError = LOCTEXT("NoProjectsFound", "No C# project found. Create one first.");
		return;
	}

	if (NewTypePath.IsEmpty())
	{
		ValidationError = LOCTEXT("NoPath", "Choose a folder for the file.");
		return;
	}

	if (!FPaths::DirectoryExists(NewTypePath))
	{
		ValidationError = FText::Format(LOCTEXT("PathMissing", "{0} doesn't exist."), FText::FromString(NewTypePath));
		return;
	}
	
	if (!ProjectDestinationPicker->GetSelectedDestination().IsValid())
	{
		ValidationError = LOCTEXT("NotInProject", "The target folder does not belong to any C# project");
		return;
	}

	const FString ManagedTypeName = GetManagedTypeName();

	if (FPaths::FileExists(GetTargetFilePath()))
	{
		ValidationError = FText::Format(LOCTEXT("FileExists", "{0}.cs already exists in this folder."), FText::FromString(ManagedTypeName));
		return;
	}

	if (SelectedTypeOption->FindConflictingType(NewTypeName, ManagedTypeName))
	{
		ValidationError = FText::Format(LOCTEXT("TypeExists", "A type named {0} is already registered."), FText::FromString(ManagedTypeName));
		return;
	}

	bIsValid = true;
}

FCSNewTypeParams SCSTypeWizard::MakeTypeParams() const
{
	FCSNewTypeParams Params;
	Params.TypeOption = SelectedTypeOption;
	Params.TypeName = NewTypeName;
	Params.ParentClass = NeedsParentClass() ? SelectedParentClass.Get() : nullptr;
	Params.Namespace = FCSTypeGenerator::DeriveNamespaceFromPath(NewTypePath);
	Params.OutputDirectory = NewTypePath;
	
	return Params;
}

FString SCSTypeWizard::GetManagedTypeName() const
{
	return SelectedTypeOption.IsValid() ? SelectedTypeOption->GetManagedTypeName(MakeTypeParams()) : NewTypeName;
}

FText SCSTypeWizard::GetTypeOptionDisplayName() const
{
	return SelectedTypeOption.IsValid() ? SelectedTypeOption->GetDisplayName() : FText::GetEmpty();
}

FText SCSTypeWizard::GetNameHintText() const
{
	return SelectedTypeOption.IsValid() ? SelectedTypeOption->GetNameHint() : FText::GetEmpty();
}

FText SCSTypeWizard::GetDeclarationText() const
{
	if (NewTypeName.IsEmpty() || !SelectedTypeOption.IsValid())
	{
		return FText::GetEmpty();
	}

	return SelectedTypeOption->GetDeclarationPreview(MakeTypeParams());
}

FText SCSTypeWizard::GetNamespaceText() const
{
	return FText::FromString(FCSTypeGenerator::DeriveNamespaceFromPath(NewTypePath));
}

FString SCSTypeWizard::GetTargetFilePath() const
{
	if (!SelectedTypeOption.IsValid())
	{
		return FString();
	}

	return FPaths::Combine(NewTypePath, SelectedTypeOption->GetFileName(MakeTypeParams()));
}

FText SCSTypeWizard::GetFilePathText() const
{
	if (NewTypeName.IsEmpty())
	{
		return FText::GetEmpty();
	}

	FString FilePath = FPaths::ConvertRelativePathToFull(GetTargetFilePath());

	const TSharedPtr<FCSProjectDestination> Destination = ProjectDestinationPicker.IsValid() ? ProjectDestinationPicker->GetSelectedDestination() : nullptr;
	if (!Destination.IsValid())
	{
		return FText::FromString(FilePath);
	}

	FString RootDirectory = FPaths::ConvertRelativePathToFull(Destination->GetRootDirectory());
	FPaths::NormalizeDirectoryName(RootDirectory);

	FString BaseDirectory = FPaths::GetPath(RootDirectory);
	if (BaseDirectory.IsEmpty())
	{
		BaseDirectory = RootDirectory;
	}
	BaseDirectory += TEXT("/");

	FString RelativePath = FilePath;
	if (!FPaths::MakePathRelativeTo(RelativePath, *BaseDirectory) || RelativePath.StartsWith(TEXT("..")))
	{
		return FText::FromString(FilePath);
	}

	return FText::FromString(RelativePath);
}

FReply SCSTypeWizard::HandleCreateClicked()
{
	if (!bIsValid)
	{
		return FReply::Handled();
	}

	const FCSNewTypeParams Params = MakeTypeParams();

	FString WrittenFilePath;
	FText FailReason;
	if (!FCSTypeGenerator::GenerateTypeFile(Params, WrittenFilePath, FailReason))
	{
		FMessageDialog::Open(EAppMsgType::Ok, 
		                     FText::Format(LOCTEXT("CreateFailed", "Couldn't create the {0}.\n\n{1}"), GetTypeOptionDisplayName().ToLower(), FailReason));
		return FReply::Handled();
	}

	OnClassCreated.ExecuteIfBound(GetManagedTypeName(), WrittenFilePath);
	CloseContainingWindow();
	
	return FReply::Handled();
}

FReply SCSTypeWizard::HandleCancelClicked()
{
	CloseContainingWindow();
	return FReply::Handled();
}

void SCSTypeWizard::CloseContainingWindow()
{
	if (const TSharedPtr<SWindow> Window = FSlateApplication::Get().FindWidgetWindow(AsShared()))
	{
		Window->RequestDestroyWindow();
	}
}

FReply SCSTypeWizard::OnKeyDown(const FGeometry& MyGeometry, const FKeyEvent& InKeyEvent)
{
	if (InKeyEvent.GetKey() == EKeys::Escape)
	{
		return HandleCancelClicked();
	}

	return SCompoundWidget::OnKeyDown(MyGeometry, InKeyEvent);
}

EActiveTimerReturnType SCSTypeWizard::SetFocusPostConstruct(double InCurrentTime, float InDeltaTime)
{
	if (NameTextBox.IsValid())
	{
		FSlateApplication::Get().SetKeyboardFocus(NameTextBox, EFocusCause::SetDirectly);
		NameTextBox->SelectAllText();
	}

	return EActiveTimerReturnType::Stop;
}

void SCSTypeWizard::OpenDialog(const FOnClassCreated& OnClassCreated)
{
	const TSharedRef<SWindow> Window = SNew(SWindow)
		.Title(LOCTEXT("WindowTitle", "New C# Type"))
		.SizingRule(ESizingRule::Autosized)
		.SupportsMinimize(false)
		.SupportsMaximize(false);

	Window->SetContent(SNew(SCSTypeWizard)
	.OnClassCreated(OnClassCreated));
	
	IMainFrameModule& MainFrame = FModuleManager::LoadModuleChecked<IMainFrameModule>("MainFrame");
	TSharedPtr<SWindow> ParentWindow = MainFrame.GetParentWindow();
	FSlateApplication::Get().AddModalWindow(Window, ParentWindow);
}

#undef LOCTEXT_NAMESPACE
