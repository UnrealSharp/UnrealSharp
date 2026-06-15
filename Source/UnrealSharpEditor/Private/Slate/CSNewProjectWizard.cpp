#include "Slate/CSNewProjectWizard.h"

#include "CSCommonGlobalSettings.h"
#include "DesktopPlatformModule.h"
#include "IDesktopPlatform.h"
#include "Interfaces/IPluginManager.h"
#include "Runtime/AppFramework/Public/Widgets/Workflow/SWizard.h"
#include "Styling/StyleColors.h"
#include "UnrealSharpEditor.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "CSUnrealSharpEditorSettings.h"

#define LOCTEXT_NAMESPACE "UnrealSharpEditor"

void SCSNewProjectDialog::Construct(const FArguments& InArgs)
{
	static FName ProjectDestination(TEXT("<ProjectDestination>"));
	const FString ScriptPath = FPaths::ConvertRelativePathToFull(UnrealSharp::Paths::GetScriptFolderDirectory());

	FText ProjectDestinationName = FText::FromString(FString::Printf(TEXT("%s (This Project)"), FApp::GetProjectName()));
	ProjectDestinations.Add(MakeShared<FCSProjectDestination>(ProjectDestination, ProjectDestinationName, FApp::GetProjectName(), ScriptPath, 0));

	IPluginManager& PluginManager = IPluginManager::Get();
	TArray<TSharedRef<IPlugin>> EnabledPlugins = PluginManager.GetEnabledPlugins();

	for (const TSharedRef<IPlugin>& Plugin : EnabledPlugins)
	{
		const FString PluginFilePath = FPaths::ConvertRelativePathToFull(Plugin->GetBaseDir());
		if (!FPaths::IsUnderDirectory(PluginFilePath, UnrealSharp::Paths::GetPluginsDirectory()) || Plugin->GetName() == UE_PLUGIN_NAME)
		{
			continue;
		}

		FString ScriptDirectory = PluginFilePath / UnrealSharp::GlobalSettings::Common::GetScriptDirectoryName();
		ProjectDestinations.Add(MakeShared<FCSProjectDestination>(FName(Plugin->GetName()),
		                                                          FText::FromString(Plugin->GetFriendlyName()), Plugin->GetName(), ScriptDirectory, ProjectDestinations.Num(), Plugin));
	}
	
	SelectedProjectDestinationIndex = 0;

	constexpr float LabelColumnWidth = 84.0f;

	auto MakeRow = [LabelColumnWidth](const FText& InLabel, const TSharedRef<SWidget>& InContent, bool bFillContent = true) -> TSharedRef<SWidget>
	{
		TSharedRef<SHorizontalBox> Row = SNew(SHorizontalBox)
			+ SHorizontalBox::Slot()
			.AutoWidth()
			.VAlign(VAlign_Center)
			.Padding(0, 0, 12, 0)
			[
				SNew(SBox)
				.WidthOverride(LabelColumnWidth)
				[
					SNew(STextBlock)
					.Text(InLabel)
					.Justification(ETextJustify::Right)
				]
			];

		if (bFillContent)
		{
			Row->AddSlot().FillWidth(1).VAlign(VAlign_Center)[ InContent ];
		}
		else
		{
			Row->AddSlot().AutoWidth().VAlign(VAlign_Center)[ InContent ];
		}
		return Row;
	};

	ChildSlot
	[
		SNew(SWizard)
		.ShowPageList(false)
		.FinishButtonText(LOCTEXT("CreateProject", "Create C# Project"))
		.FinishButtonToolTip(LOCTEXT("CreateProjectTooltip", "Create a new C# project with the specified settings"))
		.OnCanceled(this, &SCSNewProjectDialog::OnCancel)
		.OnFinished(this, &SCSNewProjectDialog::OnFinish)
		.CanFinish(this, &SCSNewProjectDialog::CanFinish)
		+ SWizard::Page()
		[
			SNew(SOverlay)
			+ SOverlay::Slot()
			.HAlign(HAlign_Center)
			.VAlign(VAlign_Center)
			[
				SNew(SBox)
				.WidthOverride(580.0f)
				[
					SNew(SVerticalBox)
					+ SVerticalBox::Slot()
					.AutoHeight()
					.Padding(0, 0, 0, 16)
					[
						SNew(SHorizontalBox)
						+ SHorizontalBox::Slot()
						.FillWidth(1)
						.VAlign(VAlign_Center)
						[
							SNew(SVerticalBox)
							+ SVerticalBox::Slot()
							.AutoHeight()
							.Padding(0, 0, 0, 4)
							[
								SNew(STextBlock)
								.Text(LOCTEXT("NewProjectTitle", "Create C# Project"))
								.Font(FCoreStyle::GetDefaultFontStyle("Bold", 15))
							]
							+ SVerticalBox::Slot()
							.AutoHeight()
							[
								SNew(STextBlock)
								.Text(LOCTEXT("NewProjectSubtitle", "Configure the settings for your new C# project."))
								.ColorAndOpacity(FSlateColor::UseSubduedForeground())
								.AutoWrapText(true)
							]
						]
					]

					+ SVerticalBox::Slot()
					.AutoHeight()
					[
						SNew(SBorder)
						.BorderImage(FAppStyle::Get().GetBrush("Brushes.Panel"))
						.Padding(18.0f)
						[
							SNew(SVerticalBox)

							+ SVerticalBox::Slot()
							.AutoHeight()
							.Padding(0, 0, 0, 12)
							[
								MakeRow(
									LOCTEXT("NewProjectName", "Name"),
									SAssignNew(NameTextBox, SEditableTextBox)
									.HintText(LOCTEXT("NewProjectNameHint", "MyProject"))
									.SelectAllTextWhenFocused(true)
									.OnTextCommitted_Lambda([this](const FText&, ETextCommit::Type CommitType)
									{
										if (CommitType == ETextCommit::OnEnter && CanFinish())
										{
											OnFinish();
										}
									})
								)
							]

							+ SVerticalBox::Slot()
							.AutoHeight()
							.Padding(0, 0, 0, 12)
							[
								MakeRow(
									LOCTEXT("NewProjectOwner", "Owner"),
									SAssignNew(ProjectDestinationComboBox, SComboBox<TSharedRef<FCSProjectDestination>>)
									.OptionsSource(&ProjectDestinations)
									.InitiallySelectedItem(ProjectDestinations[SelectedProjectDestinationIndex])
									.OnSelectionChanged(this, &SCSNewProjectDialog::OnProjectDestinationChanged)
									.OnGenerateWidget_Static(&SCSNewProjectDialog::OnGenerateProjectDestinationWidget)
									.Content()
									[
										SNew(STextBlock).Text_Lambda([this]
										{
											if (!ProjectDestinations.IsValidIndex(SelectedProjectDestinationIndex))
											{
												return FText();
											}
											return ProjectDestinations[SelectedProjectDestinationIndex]->GetDisplayName();
										})
									]
								)
							]

							+ SVerticalBox::Slot()
							.AutoHeight()
							.Padding(0, 0, 0, 12)
							[
								MakeRow(
									LOCTEXT("NewProjectLocation", "Location"),
									SNew(SHorizontalBox)
									+ SHorizontalBox::Slot()
									.FillWidth(1)
									.VAlign(VAlign_Center)
									[
										SAssignNew(PathTextBox, SEditableTextBox)
										.Text(FText::FromString(ScriptPath))
									]
									+ SHorizontalBox::Slot()
									.AutoWidth()
									.VAlign(VAlign_Center)
									.Padding(6, 0, 0, 0)
									[
										SNew(SButton)
										.VAlign(VAlign_Center)
										.ButtonStyle(FAppStyle::Get(), "SimpleButton")
										.ToolTipText(LOCTEXT("BrowseLocationTooltip", "Browse for a folder"))
										.OnClicked(this, &SCSNewProjectDialog::OnExplorerButtonClicked)
										[
											SNew(SImage)
											.Image(FAppStyle::Get().GetBrush("Icons.FolderClosed"))
											.ColorAndOpacity(FSlateColor::UseForeground())
										]
									]
								)
							]

							+ SVerticalBox::Slot()
							.AutoHeight()
							.Padding(0, 4, 0, 0)
							[
								MakeRow(
									LOCTEXT("NewProjectEditorOnly", "Editor Only"),
									SAssignNew(EditorOnlyCheckBox, SCheckBox)
									.ToolTipText(LOCTEXT(
										"EditorOnlyTooltip",
										"When enabled, the generated project is editor-only and will not be included in packaged builds. "
										"To make it available in packaged builds later, change the IsPublishable property in the .csproj file to true.")),
									false
								)
							]
						]
					]

					+ SVerticalBox::Slot()
					.AutoHeight()
					.Padding(2, 12, 2, 0)
					[
						SNew(SBox)
						.MinDesiredHeight(20.0f)
						[
							SNew(SVerticalBox)

							+ SVerticalBox::Slot()
							.AutoHeight()
							[
								SNew(SHorizontalBox)
								.Visibility_Lambda([this]
								{
									return GetValidationError().IsEmpty() ? EVisibility::Collapsed : EVisibility::Visible;
								})
								+ SHorizontalBox::Slot()
								.AutoWidth()
								.VAlign(VAlign_Center)
								.Padding(0, 0, 6, 0)
								[
									SNew(SImage)
									.Image(FAppStyle::Get().GetBrush("Icons.Warning"))
									.ColorAndOpacity(FStyleColors::Warning)
								]
								+ SHorizontalBox::Slot()
								.FillWidth(1)
								.VAlign(VAlign_Center)
								[
									SNew(STextBlock)
									.AutoWrapText(true)
									.ColorAndOpacity(FStyleColors::Warning)
									.Text_Lambda([this] { return GetValidationError(); })
								]
							]

							+ SVerticalBox::Slot()
							.AutoHeight()
							[
								SNew(STextBlock)
								.Visibility_Lambda([this]
								{
									return GetValidationError().IsEmpty() ? EVisibility::Visible : EVisibility::Collapsed;
								})
								.ColorAndOpacity(FSlateColor::UseSubduedForeground())
								.Font(FCoreStyle::GetDefaultFontStyle("Italic", 8))
								.AutoWrapText(true)
								.Text_Lambda([this]
								{
									const FString ProjectName = NameTextBox.IsValid() ? NameTextBox->GetText().ToString() : FString();
									const FString BasePath = PathTextBox.IsValid() ? PathTextBox->GetText().ToString() : FString();
									if (BasePath.IsEmpty())
									{
										return FText::GetEmpty();
									}
									return FText::Format(
										LOCTEXT("NewProjectPathPreview", "Will be created at:  {0}"),
										FText::FromString(FPaths::Combine(BasePath, ProjectName, ProjectName + TEXT(".csproj"))));
								})
							]
						]
					]
				]
			]
		]
	];

	RegisterActiveTimer(0.0f, FWidgetActiveTimerDelegate::CreateLambda([this](double, float)
	{
		if (NameTextBox.IsValid())
		{
			FSlateApplication::Get().SetKeyboardFocus(NameTextBox);
			NameTextBox->SelectAllText();
		}
		return EActiveTimerReturnType::Stop;
	}));

	OnProjectDestinationChanged(ProjectDestinations[SelectedProjectDestinationIndex], ESelectInfo::Direct);
}

void SCSNewProjectDialog::OnProjectDestinationChanged(TSharedPtr<FCSProjectDestination> NewProjectDestination, ESelectInfo::Type)
{
	if (!NewProjectDestination.IsValid())
	{
		SelectedProjectDestinationIndex = INDEX_NONE;
		return;
	}

	SelectedProjectDestinationIndex = NewProjectDestination->GetIndex();

	if (PathTextBox.IsValid())
	{
		PathTextBox->SetText(FText::FromString(NewProjectDestination->GetPath()));
	}

	const FString NewSuggestedName = TEXT("Managed") + NewProjectDestination->GetName();

	if (NameTextBox.IsValid())
	{
		const FString CurrentName = NameTextBox->GetText().ToString();
		if (CurrentName.IsEmpty() || CurrentName == SuggestedProjectName)
		{
			NameTextBox->SetText(FText::FromString(NewSuggestedName));
		}
	}

	SuggestedProjectName = NewSuggestedName;
}

TSharedRef<SWidget> SCSNewProjectDialog::OnGenerateProjectDestinationWidget(TSharedRef<FCSProjectDestination> Destination)
{
	return SNew(STextBlock)
		.Text(Destination->GetDisplayName());
}

void SCSNewProjectDialog::OnPathSelected(const FString& NewPath)
{
	if (NewPath.IsEmpty())
	{
		return;
	}

	PathTextBox->SetText(FText::FromString(NewPath));
}

FReply SCSNewProjectDialog::OnExplorerButtonClicked()
{
	IDesktopPlatform* DesktopPlatform = FDesktopPlatformModule::Get();

	if (!DesktopPlatform)
	{
		return FReply::Handled();
	}

	TSharedPtr<SWindow> ParentWindow = FSlateApplication::Get().FindWidgetWindow(AsShared());
	void* ParentWindowWindowHandle = ParentWindow.IsValid() ? ParentWindow->GetNativeWindow()->GetOSWindowHandle() : nullptr;

	const FString DefaultPath = PathTextBox.IsValid() ? PathTextBox->GetText().ToString() : UnrealSharp::Paths::GetScriptFolderDirectory();

	FString FolderName;
	const FString Title = LOCTEXT("ChooseProjectLocation", "Choose a location for the new project").ToString();
	if (DesktopPlatform->OpenDirectoryDialog(ParentWindowWindowHandle, Title, DefaultPath, FolderName))
	{
		if (!FolderName.EndsWith(TEXT("/")))
		{
			FolderName += TEXT("/");
		}
	}

	if (FolderName.IsEmpty())
	{
		return FReply::Handled();
	}

	PathTextBox->SetText(FText::FromString(FolderName));

	return FReply::Handled();
}

void SCSNewProjectDialog::OnCancel()
{
	CloseWindow();
}

void SCSNewProjectDialog::OnFinish()
{
	if (!CanFinish())
	{
		return;
	}

	FString ProjectRoot;
	if (ProjectDestinations.IsValidIndex(SelectedProjectDestinationIndex) && SelectedProjectDestinationIndex > 0)
	{
		const TSharedRef<FCSProjectDestination>& Destination = ProjectDestinations[SelectedProjectDestinationIndex];
		ProjectRoot = Destination->GetPlugin()->GetBaseDir();
	}
	else
	{
		ProjectRoot = FPaths::ProjectDir();
	}

	TMap<FString, FString> Arguments;

	if (EditorOnlyCheckBox->IsChecked())
	{
		Arguments.Add(TEXT("EditorOnly"), TEXT("true"));
	}

	Arguments.Add(TEXT("CreateModuleClass"), TEXT("true"));

	FString ModuleName = NameTextBox->GetText().ToString();
	FString ProjectParentFolder = PathTextBox->GetText().ToString();

	FUnrealSharpEditorModule::Get().AddNewProject(ModuleName, ProjectParentFolder, ProjectRoot, Arguments);
	CloseWindow();
}

FText SCSNewProjectDialog::GetValidationError() const
{
	const FString Name = NameTextBox.IsValid() ? NameTextBox->GetText().ToString() : FString();
	const FString Path = PathTextBox.IsValid() ? PathTextBox->GetText().ToString() : FString();

	if (Name.IsEmpty())
	{
		return LOCTEXT("NameErrorEmpty", "Enter a project name.");
	}

	if (FChar::IsDigit(Name[0]))
	{
		return LOCTEXT("NameErrorLeadingDigit", "The name must start with a letter or underscore.");
	}

	for (const TCHAR Character : Name)
	{
		if (!FChar::IsAlnum(Character) && Character != TEXT('_') && Character != TEXT('.'))
		{
			return LOCTEXT("NameErrorChars", "Use only letters, numbers, underscores, or dots.");
		}
	}

	const FString Filename = Name + TEXT(".csproj");
	if (FPaths::MakeValidFileName(Filename) != Filename)
	{
		return LOCTEXT("NameErrorInvalidFile", "The name contains characters that are not allowed in a file name.");
	}
	
	const UCSUnrealSharpEditorSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();
	const int32 MaxLength = Settings->MaxProjectNameLength;

	if (Filename.Len() > MaxLength)
	{
		return FText::Format(
			LOCTEXT("ProjectNameTooLong", "Project name is too long. The maximum allowed length is {0} characters (current length: {1})."),
			FText::AsNumber(MaxLength),
			FText::AsNumber(Filename.Len())
		);
	}

	if (UnrealSharp::Project::IsAssemblyInAnyManifest(Name))
	{
		return FText::Format(LOCTEXT("NameErrorExists", "A project named \"{0}\" already exists."), FText::FromString(Name));
	}

	if (Path.IsEmpty())
	{
		return LOCTEXT("PathErrorEmpty", "Choose a location for the project.");
	}

	if (!ProjectDestinations.IsValidIndex(SelectedProjectDestinationIndex))
	{
		return LOCTEXT("OwnerErrorNone", "Select an owner for the project.");
	}

	const FString DestinationPath = ProjectDestinations[SelectedProjectDestinationIndex]->GetPath();
	if (!Path.Contains(DestinationPath))
	{
		return FText::Format(
			LOCTEXT("PathErrorOutsideOwner", "The location must be inside the selected owner's folder:\n{0}"),
			FText::FromString(DestinationPath));
	}

	return FText::GetEmpty();
}

bool SCSNewProjectDialog::CanFinish() const
{
	return GetValidationError().IsEmpty();
}

void SCSNewProjectDialog::CloseWindow()
{
	TSharedPtr<SWindow> ContainingWindow = FSlateApplication::Get().FindWidgetWindow(AsShared());
	if (ContainingWindow.IsValid())
	{
		ContainingWindow->RequestDestroyWindow();
	}
}

#undef LOCTEXT_NAMESPACE
