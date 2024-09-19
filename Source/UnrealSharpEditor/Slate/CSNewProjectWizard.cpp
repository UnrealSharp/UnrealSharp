#include "CSNewProjectWizard.h"
#include "DesktopPlatformModule.h"
#include "IDesktopPlatform.h"
#include "Runtime/AppFramework/Public/Widgets/Workflow/SWizard.h"
#include "UnrealSharpEditor/UnrealSharpEditor.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"

#define LOCTEXT_NAMESPACE "UnrealSharpEditor"

void SCSNewProjectDialog::Construct(const FArguments& InArgs)
{
	ScriptPath = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetScriptFolderDirectory());
	SuggestedProjectName = InArgs._SuggestedProjectName.Get(FString());
	
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
				SNew(SVerticalBox)
				+ SVerticalBox::Slot()
				.Padding(0, 0, 0, 10)
				[
					SNew(SHorizontalBox)
					+ SHorizontalBox::Slot()
					.AutoWidth()
					.VAlign(VAlign_Center)
					.Padding(0, 0, 10, 0)
					[
						SNew(STextBlock)
						.Text(LOCTEXT("NewProjectName", "Name"))
					]
					+ SHorizontalBox::Slot()
					.FillWidth(1)
					[
						SAssignNew(NameTextBox, SEditableTextBox)
						.Text(FText::FromString(SuggestedProjectName))
					]
				]
				+ SVerticalBox::Slot()
				.Padding(0, 0, 0, 10)
				[
					SNew(SHorizontalBox)
					+ SHorizontalBox::Slot()
					.AutoWidth()
					.VAlign(VAlign_Center)
					.Padding(0, 0, 10, 0)
					[
						SNew(STextBlock)
						.Text(LOCTEXT("NewProjectLocation", "Location"))
					]
					+ SHorizontalBox::Slot()
					.FillWidth(1)
					[
						SAssignNew(PathTextBox, SEditableTextBox)
						.Text(FText::FromString(ScriptPath))
					]
					+ SHorizontalBox::Slot()
					.AutoWidth()
					[
						SNew(SButton)
						.VAlign(VAlign_Center)
						.ButtonStyle(FAppStyle::Get(), "SimpleButton")
						.OnClicked(this, &SCSNewProjectDialog::OnExplorerButtonClicked)
						[
							SNew(SImage)
							.Image(FAppStyle::Get().GetBrush("Icons.FolderClosed"))
							.ColorAndOpacity(FSlateColor::UseForeground())
						]
					]
				]
			]
		]
	];
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

	FString FolderName;
	const FString Title = TEXT("Choose a location for new project");
	if (DesktopPlatform->OpenDirectoryDialog(ParentWindowWindowHandle, TEXT(""),FCSProcHelper::GetScriptFolderDirectory(), FolderName))
	{
		if (!FolderName.EndsWith(TEXT("/")) )
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
	TMap<FString, FString> Arguments;
	Arguments.Add("NewProjectName", NameTextBox->GetText().ToString());
	Arguments.Add("NewProjectPath", PathTextBox->GetText().ToString());
	FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_GENERATE_PROJECT, Arguments);
	FUnrealSharpEditorModule::OpenSolution();
	CloseWindow();
}

bool SCSNewProjectDialog::CanFinish() const
{
	FString Name = NameTextBox->GetText().ToString();
	FString Path = PathTextBox->GetText().ToString();
	FString Filename = Name + ".csproj";
	FString AbsolutePath = Path / Filename;

	// Path can't be empty, name can't be empty, and path must contain the script path
	if (Path.IsEmpty() || Name.IsEmpty() || !Path.Contains(ScriptPath))
	{
		return false;
	}

	// Name can't contain spaces
	if (Name.Contains(TEXT(" ")))
	{
		return false;
	}

	// Path must be a valid directory
	if (FPaths::DirectoryExists(Path / Name))
	{
		return false;
	}

	// File must not already exist
	IFileManager& FileManager = IFileManager::Get();
	TArray<FString> AssemblyPaths;
	FileManager.FindFiles(AssemblyPaths, *Path, TEXT(".csproj"));

	for (const FString& AssemblyPath : AssemblyPaths)
	{
		FString ProjectName = FPaths::GetBaseFilename(AssemblyPath);
		if (ProjectName == Name)
		{
			return false;
		}
	}

	return true;
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
