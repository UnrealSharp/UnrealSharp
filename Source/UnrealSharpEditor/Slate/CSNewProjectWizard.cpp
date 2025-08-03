#include "CSNewProjectWizard.h"

#include "CSManager.h"
#include "DesktopPlatformModule.h"
#include "IDesktopPlatform.h"
#include "Interfaces/IPluginManager.h"
#include "Runtime/AppFramework/Public/Widgets/Workflow/SWizard.h"
#include "UnrealSharpEditor/UnrealSharpEditor.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"

#define LOCTEXT_NAMESPACE "UnrealSharpEditor"

void SCSNewProjectDialog::Construct(const FArguments& InArgs)
{
    static FName ProjectDestination(TEXT("<ProjectDestination>"));
	const FString ScriptPath = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetScriptFolderDirectory());

	FText ProjectDestinationName = FText::FromString(FString::Printf(TEXT("%s (This Project)"), FApp::GetProjectName()));
    ProjectDestinations.Add(MakeShared<FCSProjectDestination>(ProjectDestination, ProjectDestinationName, FApp::GetProjectName(), ScriptPath, 0));
	
    IPluginManager& PluginManager = IPluginManager::Get();
    TArray<TSharedRef<IPlugin>> EnabledPlugins = PluginManager.GetEnabledPlugins();

    for (const TSharedRef<IPlugin>& Plugin : EnabledPlugins)
    {
        const FString PluginFilePath = FPaths::ConvertRelativePathToFull(Plugin->GetBaseDir());
        if (!FPaths::IsUnderDirectory(PluginFilePath, FCSProcHelper::GetPluginsDirectory()) || Plugin->GetName() == UE_PLUGIN_NAME)
        {
            continue;
        }
    	
        FString ScriptDirectory = PluginFilePath / "Script";
        ProjectDestinations.Add(MakeShared<FCSProjectDestination>(FName(Plugin->GetName()),
            FText::FromString(Plugin->GetFriendlyName()), Plugin->GetName(), ScriptDirectory, ProjectDestinations.Num(), Plugin));
    }
    SelectedProjectDestinationIndex = 0;

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
						.Text(LOCTEXT("NewProjectOwner", "Owner"))
					]
					+ SHorizontalBox::Slot()
					.FillWidth(1)
					[
						SAssignNew(ProjectDestinationComboBox, SComboBox<TSharedRef<FCSProjectDestination>>)
					    .OptionsSource(&ProjectDestinations)
					    .InitiallySelectedItem(ProjectDestinations[SelectedProjectDestinationIndex])
					    .OnSelectionChanged(this, &SCSNewProjectDialog::OnProjectDestinationChanged)
					    .OnGenerateWidget_Static(&SCSNewProjectDialog::OnGenerateProjectDestinationWidget)
					    .Content()
                        [
                            SNew(STextBlock).Text_Lambda([this]
                            {
                                if (SelectedProjectDestinationIndex == INDEX_NONE)
                                {
                                    return FText();
                                }

                                return ProjectDestinations[SelectedProjectDestinationIndex]->GetDisplayName();
                            })
                        ]
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

	OnProjectDestinationChanged(ProjectDestinations[SelectedProjectDestinationIndex], ESelectInfo::Direct);
}

void SCSNewProjectDialog::OnProjectDestinationChanged(TSharedPtr<FCSProjectDestination> NewProjectDestination, ESelectInfo::Type)
{
    if (NewProjectDestination == nullptr)
    {
        SelectedProjectDestinationIndex = INDEX_NONE;
        return;
    }

    SelectedProjectDestinationIndex = NewProjectDestination->GetIndex();
    PathTextBox->SetText(FText::FromString(NewProjectDestination->GetPath()));
	
	FString ManagedName = TEXT("Managed") + NewProjectDestination->GetName();
	NameTextBox->SetText(FText::FromString(ManagedName));
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

	FString FolderName;
	const FString Title = TEXT("Choose a location for new project");
	if (DesktopPlatform->OpenDirectoryDialog(ParentWindowWindowHandle, Title,FCSProcHelper::GetScriptFolderDirectory(), FolderName))
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

FString MakeQuotedPath(const FString& Path)
{
	if (Path.IsEmpty())
	{
		return TEXT("");
	}

	if (Path.StartsWith(TEXT("\"")) && Path.EndsWith(TEXT("\"")))
	{
		return Path;
	}

	return FString::Printf(TEXT("\"%s\""), *Path);
}

void SCSNewProjectDialog::OnFinish()
{
	TMap<FString, FString> Arguments;
	FString ModuleName = NameTextBox->GetText().ToString();
	FString ProjectParentFolder = PathTextBox->GetText().ToString();

	TMap<FString, FString> SolutionArguments;
	SolutionArguments.Add(TEXT("MODULENAME"), ModuleName);

	FString ProjectFolder = FPaths::Combine(ProjectParentFolder, ModuleName);
	FString ModuleFilePath = FPaths::Combine(ProjectFolder, ModuleName + ".cs");
	
	FUnrealSharpEditorModule::FillTemplateFile(TEXT("Module"), SolutionArguments, ModuleFilePath);

	Arguments.Add(TEXT("NewProjectName"), ModuleName);
	Arguments.Add(TEXT("NewProjectFolder"), MakeQuotedPath(FPaths::ConvertRelativePathToFull(ProjectParentFolder)));

	FString ProjectRoot;
    if (ProjectDestinations.IsValidIndex(SelectedProjectDestinationIndex))
    {
    	const TSharedRef<FCSProjectDestination>& Destination = ProjectDestinations[SelectedProjectDestinationIndex];
    	const TSharedPtr<IPlugin>& Plugin = Destination->GetPlugin();
    	
        const FString& GlueProjectName = Arguments.Add(TEXT("GlueProjectName"), FString::Printf(TEXT("%s.PluginGlue"), *Plugin->GetName()));
        const FString GlueProjectLocation = FPaths::Combine(Destination->GetPath(), GlueProjectName, FString::Printf(TEXT("%s.csproj"), *GlueProjectName));

    	ProjectRoot = Plugin->GetBaseDir();
    	
        if (!FPaths::FileExists(GlueProjectLocation))
        {
            Arguments.Add(TEXT("SkipIncludeProjectGlue"), TEXT("true"));
        }
    }
    else
    {
    	ProjectRoot = FPaths::ProjectDir();
    }

	ProjectRoot = FPaths::ConvertRelativePathToFull(ProjectRoot);
	Arguments.Add(TEXT("ProjectRoot"), MakeQuotedPath(ProjectRoot));

	if (!FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_GENERATE_PROJECT, Arguments))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Failed to generate project %s in %s", *ModuleName, *ProjectParentFolder);
		return;
	}

	FUnrealSharpEditorModule& UnrealSharpEditor = FUnrealSharpEditorModule::Get();
	UnrealSharpEditor.OpenSolution();
	UnrealSharpEditor.AddDirectoryToWatch(FPaths::Combine(ProjectRoot, TEXT("Script")));

	FString CsProjPath = FPaths::Combine(ProjectFolder, ModuleName + ".csproj");

	if (!FPaths::FileExists(CsProjPath))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Failed to find .csproj %s in %s", *ModuleName, *ProjectParentFolder);
		return;
	}
	
	UnrealSharpEditor.GetManagedUnrealSharpEditorCallbacks().AddProjectToCollection(*CsProjPath);
	
	CloseWindow();
}

bool SCSNewProjectDialog::CanFinish() const
{
	FString Name = NameTextBox->GetText().ToString();
	FString Path = PathTextBox->GetText().ToString();
	FString Filename = Name + ".csproj";
	FString AbsolutePath = Path / Filename;

	// Path can't be empty, name can't be empty, and path must contain the script path
	if (Path.IsEmpty() || Name.IsEmpty() || SelectedProjectDestinationIndex == INDEX_NONE
	    || !Path.Contains(ProjectDestinations[SelectedProjectDestinationIndex]->GetPath()))
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
